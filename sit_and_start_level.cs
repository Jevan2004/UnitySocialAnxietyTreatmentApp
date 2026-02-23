using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SitAndStartLevel : MonoBehaviour
{
    [Header("Setup")]
    public Transform sitPosition;
    public PlayerController playerController;
    public GameObject nextLevelBarrier;

    [Header("Level 3 - Girl")]
    public GameObject girlObject;

    [Header("System Links")]
    public NpcSpawner spawner;

    [Header("Settings")]
    public float interactionDistance = 3f;
    public float girlSpawnDelay = 4.0f;
    public float timeToCompleteLevel1 = 20f;
    public float timeToCompleteLevel2 = 20f;
    public float timeToCompleteLevel3 = 20f;

    private bool isSitting = false;
    private int currentLevel = 0;
    private bool canAdvance = false;
    [SerializeField] private int savedLevel = 1;

    private Vector3 originalPos;
    private float currentTimer = 0f;
    private Camera playerCam;

    void Start()
    {
        savedLevel = 1;

        if (girlObject != null)
            girlObject.SetActive(false);

        if (playerController != null)
            playerCam = playerController.GetComponentInChildren<Camera>();

        NPCStudent.isLevel2Active = false;
        NPCStudent.isLevel3Active = false;
    }

    void Update()
    {
        if (!isSitting)
            CheckForChairInteraction();
        else
            RunSessionLogic();
    }

    void CheckForChairInteraction()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
            {
                if (hit.transform == transform)
                    StartSession();
            }
        }
    }

    void StartSession()
    {
        isSitting = true;
        canAdvance = false;
        currentTimer = 0f;

        NPCStudent.isLevel2Active = false;
        NPCStudent.isLevel3Active = false;

        playerController.canMove = false;
        Rigidbody rb = playerController.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        originalPos = playerController.transform.position;
        playerController.transform.position = sitPosition.position;

        if (spawner != null)
            spawner.StartLevel();

        if (savedLevel == 3) StartLevel3();
        else if (savedLevel == 2) StartLevel2();
        else
        {
            currentLevel = 1;
        }
    }

    void RunSessionLogic()
    {
        currentTimer += Time.deltaTime;

        if (currentLevel == 1)
        {
            if (currentTimer >= timeToCompleteLevel1)
                canAdvance = true;

            if (canAdvance && Keyboard.current.iKey.wasPressedThisFrame)
                StartLevel2();
        }
        else if (currentLevel == 2)
        {
            if (currentTimer >= timeToCompleteLevel2)
                canAdvance = true;

            if (canAdvance && Keyboard.current.iKey.wasPressedThisFrame)
                StartLevel3();
        }
        else if (currentLevel == 3)
        {
            if (currentTimer >= timeToCompleteLevel3 && !canAdvance)
            {
                if (girlObject != null)
                {
                    var girl = girlObject.GetComponent<GirlScript>();
                    if (girl != null)
                        girl.LeaveRoom();
                }
                canAdvance = true;
            }

            if (canAdvance && Keyboard.current.iKey.wasPressedThisFrame)
                VictoryLeave();
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            StandUp();
    }

    void StartLevel2()
    {
        currentLevel = 2;
        currentTimer = 0f;
        canAdvance = false;

        NPCStudent.isLevel2Active = true;

        if (spawner != null)
            spawner.ActivateLevel2();
    }

    void StartLevel3()
    {
        currentLevel = 3;
        currentTimer = 0f;
        canAdvance = false;

        NPCStudent.isLevel2Active = true;
        NPCStudent.isLevel3Active = true;

        if (spawner != null)
            spawner.ActivateLevel3();

        StartCoroutine(WakeUpGirl());
    }

    IEnumerator WakeUpGirl()
    {
        yield return new WaitForSeconds(girlSpawnDelay);

        if (currentLevel == 3 && girlObject != null)
            girlObject.SetActive(true);
    }

    void VictoryLeave()
    {
        savedLevel = 3;

        if (nextLevelBarrier != null)
            Destroy(nextLevelBarrier);

        StandUp();
    }

    void StandUp()
    {
        StopAllCoroutines();

        if (girlObject != null)
            girlObject.SetActive(false);

        isSitting = false;
        currentLevel = 0;
        canAdvance = false;
        currentTimer = 0f;

        NPCStudent.isLevel2Active = false;
        NPCStudent.isLevel3Active = false;

        if (spawner != null)
            spawner.StopLevel();

        playerController.transform.position = originalPos;
        playerController.canMove = true;

        Rigidbody rb = playerController.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;
    }
}