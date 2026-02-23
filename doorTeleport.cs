using UnityEngine;
using UnityEngine.SceneManagement; 

public class doorTeleport : MonoBehaviour
{
    [Tooltip("FirstStageScene")]
    public string sceneToLoad;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Loading New Scene: " + sceneToLoad);

            SceneManager.LoadScene(sceneToLoad);
        }
    }
}