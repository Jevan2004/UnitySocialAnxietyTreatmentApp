using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NPCStudent : MonoBehaviour
{
    public static bool isLevel2Active = false;
    public static bool isLevel3Active = false;

    public NpcSpawner spawner;

    [Header("Destinations")]
    public Transform mySeat;
    public Transform exitDoor;

    [Header("Behavior")]
    public float minStayTime = 10f;
    public float maxStayTime = 40f;

    [Header("Staring Settings")]
    public float minStareWait = 3f;
    public float maxStareWait = 8f;
    public float stareDuration = 3f;

    private NavMeshAgent agent;
    private Animator animator;
    private Transform playerHead;

    private bool isLeaving = false;
    private bool hasSat = false;
    private bool startedWalking = false;

    private float lookWeight = 0f;

    void OnEnable()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (Camera.main != null)
        {
            playerHead = Camera.main.transform;
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerHead = player.transform;
        }

        isLeaving = false;
        hasSat = false;
        startedWalking = false;
        lookWeight = 0f;

        if (mySeat != null) Invoke("WalkToSeat", 0.1f);
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !hasSat || isLeaving || playerHead == null)
        {
            animator.SetLookAtWeight(0);
            return;
        }

        float currentMaxAngle = isLevel2Active ? 170f : 100f;
        float currentBodyWeight = 0f;
        float currentClamp = isLevel2Active ? 0.0f : 0.5f;

        if (!IsPlayerInFront(currentMaxAngle))
        {
            animator.SetLookAtWeight(0);
            return;
        }

        Vector3 targetPos = playerHead.position;
        if (playerHead.name != "Main Camera") targetPos += Vector3.up * 1.5f;

        animator.SetLookAtWeight(lookWeight, currentBodyWeight, 1.0f, 1.0f, currentClamp);
        animator.SetLookAtPosition(targetPos);
    }

    bool IsPlayerInFront(float maxAngle)
    {
        if (playerHead == null) return false;
        Vector3 directionToPlayer = playerHead.position - transform.position;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        return angle < maxAngle;
    }

    IEnumerator StareRoutine()
    {
        while (hasSat && !isLeaving)
        {
            float wait = Random.Range(minStareWait, maxStareWait);
            yield return new WaitForSeconds(wait);

            float currentMaxAngle = isLevel2Active ? 170f : 100f;

            if (!IsPlayerInFront(currentMaxAngle)) continue;

            float timer = 0f;
            while (timer < 0.6f && !isLeaving)
            {
                timer += Time.deltaTime;
                lookWeight = Mathf.Lerp(0f, 1f, timer / 0.6f);
                yield return null;
            }

            yield return new WaitForSeconds(stareDuration);

            timer = 0f;
            while (timer < 0.6f && !isLeaving)
            {
                timer += Time.deltaTime;
                lookWeight = Mathf.Lerp(1f, 0f, timer / 0.6f);
                yield return null;
            }
        }
    }

    public void WalkToSeat()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponent<Animator>();

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(mySeat.position);

            if (animator != null)
            {
                animator.SetBool("IsWalking", true);
                animator.SetBool("IsSitting", false);
            }
            startedWalking = true;
        }
    }

    void Update()
    {
        if (agent == null || !startedWalking) return;
        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                if (isLeaving) gameObject.SetActive(false);
                else if (!hasSat) SitDown();
            }
        }
    }

    void SitDown()
    {
        hasSat = true;
        startedWalking = false;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            agent.radius = 0.01f;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        if (mySeat != null)
        {
            transform.position = mySeat.position;
            transform.rotation = mySeat.rotation;
        }

        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsSitting", true);
        }

        StartCoroutine(WaitAndLeave());
        StartCoroutine(StareRoutine());
    }

    IEnumerator WaitAndLeave()
    {
        float waitTime = Random.Range(minStayTime, maxStayTime);
        yield return new WaitForSeconds(waitTime);
        LeaveRoom();
    }

    public void LeaveRoom()
    {
        isLeaving = true;
        if (spawner != null && mySeat != null) spawner.FreeUpSeat(mySeat);

        if (agent != null)
        {
            agent.radius = 0.5f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        if (animator != null)
        {
            animator.SetBool("IsSitting", false);
            animator.SetBool("IsWalking", true);
        }

        if (agent != null && exitDoor != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(exitDoor.position);
            startedWalking = true;
        }
    }
}