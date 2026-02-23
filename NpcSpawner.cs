using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject studentPrefab;
    public Transform spawnPoint;
    public Transform exitPoint;

    [Header("Seat Zones")]
    public Transform[] farSeats;
    public Transform[] closeSeats;

    [Header("Traffic Settings")]
    public float minSpawnTime = 8f;
    public float maxSpawnTime = 15f;

    private List<Transform> activeSeatPool = new List<Transform>();
    private List<Transform> occupiedSeats = new List<Transform>();
    private List<GameObject> activeStudents = new List<GameObject>();

    private bool isSpawning = false;

    void Start() { }

    public void stopSpawning()
    {
        isSpawning = false;
        StopAllCoroutines();
    }

    public void StartLevel()
    {
        minSpawnTime = 7f;
        maxSpawnTime = 15f;

        occupiedSeats.Clear();
        activeSeatPool.Clear();
        activeSeatPool.AddRange(farSeats);

        activeStudents.Clear();

        isSpawning = true;

        StopAllCoroutines();
        StartCoroutine(SpawnRoutine());
    }

    public void StopLevel()
    {
        stopSpawning();

        foreach (GameObject student in activeStudents)
        {
            if (student != null) Destroy(student);
        }
        activeStudents.Clear();
        occupiedSeats.Clear();
    }

    IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(2f);

        while (isSpawning)
        {
            SpawnStudent();

            float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(waitTime);
        }
    }

    void SpawnStudent()
    {
        List<Transform> freeSeats = new List<Transform>();

        foreach (Transform seat in activeSeatPool)
        {
            if (!occupiedSeats.Contains(seat))
            {
                freeSeats.Add(seat);
            }
        }

        if (freeSeats.Count == 0) return;

        Transform chosenSeat = freeSeats[Random.Range(0, freeSeats.Count)];

        if (chosenSeat != null)
        {
            occupiedSeats.Add(chosenSeat);

            GameObject newStudent = Instantiate(studentPrefab, spawnPoint.position, spawnPoint.rotation);

            activeStudents.Add(newStudent);

            NPCStudent studentScript = newStudent.GetComponent<NPCStudent>();

            if (studentScript != null)
            {
                studentScript.mySeat = chosenSeat;
                studentScript.exitDoor = exitPoint;
                studentScript.spawner = this;
                studentScript.WalkToSeat();
            }
        }
    }

    public void FreeUpSeat(Transform seat)
    {
        if (occupiedSeats.Contains(seat))
        {
            occupiedSeats.Remove(seat);
        }
    }

    public void ActivateLevel2()
    {
        if (!activeSeatPool.Contains(closeSeats[0]))
        {
            activeSeatPool.AddRange(closeSeats);
        }

        minSpawnTime = 4f;
        maxSpawnTime = 9f;

        StopCoroutine("SpawnRoutine");
        StartCoroutine("SpawnRoutine");
    }

    public void ActivateLevel3()
    {
        if (!activeSeatPool.Contains(closeSeats[0]))
        {
            activeSeatPool.AddRange(closeSeats);
        }

        minSpawnTime = 6f;
        maxSpawnTime = 10f;

        StopCoroutine("SpawnRoutine");
        StartCoroutine("SpawnRoutine");
    }
}