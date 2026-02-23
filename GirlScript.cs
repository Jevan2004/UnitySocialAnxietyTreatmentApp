using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AudioSource))]
public class GirlScript : MonoBehaviour
{
    [Header("Movement Targets")]
    public Transform targetSpot; 
    public Transform exitPoint;  

    private NavMeshAgent agent;
    private Animator animator;

    [Header("Audio Setup")]
    public AudioSource audioSource;
    public AudioClip clipArrival;
    public AudioClip clipSit;
    public AudioClip clipGoodbye;

    [Header("Small Talk Loop")]
    public AudioClip[] chatClips;

    [Header("Natural Head Movement")]
    public float headJitterSpeed = 0.5f;
    public float headJitterAmount = 0.1f; 
    private Vector3 currentJitter = Vector3.zero;
    private Vector3 targetJitter = Vector3.zero;
    private float jitterTimer = 0f;

    private bool hasArrived = false;
    private bool isSitting = false;
    private bool isLeaving = false;

    private Transform playerHead;
    private float lookWeight = 0f;
    private Vector3 lookTargetOffset = Vector3.zero; 
    private bool shouldLookAtPlayer = false;

    private float pathCalculationTimer = 0f;
    private float leavingSafetyTimer = 0f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (Camera.main != null) playerHead = Camera.main.transform;

        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void OnEnable()
    {
        hasArrived = false;
        isSitting = false;
        isLeaving = false;
        shouldLookAtPlayer = false;

        lookWeight = 0f;
        pathCalculationTimer = 0f;
        leavingSafetyTimer = 0f;

        StopAllCoroutines();

        if (agent != null)
        {
            agent.Warp(startPosition);
            agent.transform.rotation = startRotation;
            agent.enabled = true;
            agent.isStopped = false;

            if (targetSpot != null)
                agent.SetDestination(targetSpot.position);
        }

        if (animator != null)
        {
            animator.Rebind();
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsSitting", false);
        }
    }

    void Update()
    {
        if (animator != null && agent != null && agent.enabled && !isSitting)
        {
            bool isActuallyMoving = agent.velocity.sqrMagnitude > 0.1f;
            animator.SetBool("IsWalking", isActuallyMoving);
        }

        if (!hasArrived && !isLeaving && agent.enabled)
        {
            pathCalculationTimer += Time.deltaTime;
            if (pathCalculationTimer > 1.0f)
            {
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                {
                    if (agent.velocity.sqrMagnitude == 0f)
                        StartCoroutine(InteractionSequence());
                }
            }
        }

        if (isLeaving)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                leavingSafetyTimer += Time.deltaTime;

                if (leavingSafetyTimer > 2.0f)
                {
                    if (!agent.pathPending && agent.remainingDistance <= 1.0f)
                    {
                        gameObject.SetActive(false); 
                    }
                }
            }
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || playerHead == null || isLeaving)
        {
            animator.SetLookAtWeight(0);
            return;
        }

        jitterTimer += Time.deltaTime;
        if (jitterTimer > 2.5f)
        {
            jitterTimer = 0f;
            targetJitter = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.1f, 0.1f), 0) * headJitterAmount;
        }
        currentJitter = Vector3.Lerp(currentJitter, targetJitter, Time.deltaTime * headJitterSpeed);

        float targetWeight = shouldLookAtPlayer ? 1.0f : 0.0f;
        lookWeight = Mathf.Lerp(lookWeight, targetWeight, Time.deltaTime * 2f);

        if (lookWeight > 0.01f)
        {
            Vector3 finalTarget = playerHead.position + lookTargetOffset + currentJitter;

            animator.SetLookAtWeight(lookWeight, 0.2f, 0.5f, 0.7f, 0.5f);
            animator.SetLookAtPosition(finalTarget);
        }
    }

    IEnumerator InteractionSequence()
    {
        if (hasArrived) yield break;
        hasArrived = true;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.enabled = false;

        Vector3 directionToPlayer = (playerHead.position - transform.position).normalized;
        directionToPlayer.y = 0;
        transform.rotation = Quaternion.LookRotation(directionToPlayer);

        animator.SetBool("IsWalking", false);
        shouldLookAtPlayer = true;

        yield return StartCoroutine(PlayTalkingClip(clipArrival));

        yield return new WaitForSeconds(3f);

        shouldLookAtPlayer = false;
        if (targetSpot != null)
        {
            animator.SetBool("IsWalking", true);
            float walkSpeed = 2.0f;
            float distance = Vector3.Distance(transform.position, targetSpot.position);

            while (distance > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetSpot.position, walkSpeed * Time.deltaTime);
                Quaternion targetRot = Quaternion.LookRotation(targetSpot.position - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);

                distance = Vector3.Distance(transform.position, targetSpot.position);
                yield return null;
            }
            transform.position = targetSpot.position;
            transform.rotation = targetSpot.rotation;
        }

        animator.SetBool("IsWalking", false);
        isSitting = true;
        animator.SetBool("IsSitting", true);
        shouldLookAtPlayer = true;

        yield return StartCoroutine(PlayTalkingClip(clipSit));
        yield return new WaitForSeconds(2f);

        if (chatClips.Length > 0)
        {
            animator.SetBool("IsTalking", true); 

            for (int i = 0; i < chatClips.Length; i++)
            {
                StartCoroutine(PlayTalkingClip(chatClips[i]));

                if (Random.value > 0.3f)
                {
                    StartCoroutine(LookAroundRandomly());
                }

                yield return new WaitForSeconds(chatClips[i].length + 0.5f);
                yield return new WaitForSeconds(2.0f); 
            }
        }
        animator.SetBool("IsTalking", false);

        yield return new WaitForSeconds(2.0f);
        LeaveRoom();
    }

    IEnumerator PlayTalkingClip(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(clip);
        }
        yield return null;
    }

    IEnumerator LookAroundRandomly()
    {

        Vector3[] lookPoints = new Vector3[]
        {
            new Vector3(1.5f, 0, 0),   
            new Vector3(-1.5f, 0, 0),  
            new Vector3(0, -1.0f, 0.5f), 
            new Vector3(2.0f, 0.5f, 0) 
        };

        Vector3 selectedOffset = lookPoints[Random.Range(0, lookPoints.Length)];

        float timer = 0f;
        while (timer < 1.0f)
        {
            timer += Time.deltaTime * 2f;
            lookTargetOffset = Vector3.Lerp(Vector3.zero, selectedOffset, timer);
            yield return null;
        }

        yield return new WaitForSeconds(Random.Range(1.5f, 3.0f));

        timer = 0f;
        while (timer < 1.0f)
        {
            timer += Time.deltaTime * 2f;
            lookTargetOffset = Vector3.Lerp(selectedOffset, Vector3.zero, timer);
            yield return null;
        }
        lookTargetOffset = Vector3.zero;
    }

    public void LeaveRoom()
    {
        if (isLeaving) return;
        isLeaving = true;
        leavingSafetyTimer = 0f; 

        if (exitPoint == null)
        {
            Debug.LogError("No Exit Point!");
            return;
        }

        StopAllCoroutines();
        animator.SetBool("IsTalking", false);
        StartCoroutine(LeaveSequence());
    }

    IEnumerator LeaveSequence()
    {
        shouldLookAtPlayer = true;
        animator.SetBool("Gesture", true);

        audioSource.Stop();
        if (clipGoodbye != null) audioSource.PlayOneShot(clipGoodbye);

        yield return new WaitForSeconds(2.5f);
        animator.SetBool("Gesture", false);

        shouldLookAtPlayer = false;
        isSitting = false;
        animator.SetBool("IsSitting", false);

        yield return new WaitForSeconds(1.5f);

        animator.SetBool("IsWalking", true);

        agent.enabled = true;
        agent.isStopped = false;

        yield return null; 

        if (exitPoint != null && agent.isOnNavMesh)
        {
            agent.SetDestination(exitPoint.position);
        }
    }
}