using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndRunAndQuit : MonoBehaviour
{
    [Header("Good Job Scene")]
    public string goodJobSceneName = "GoodJob";

    [Header("Timing")]
    public float quitDelaySeconds = 3f;

    private bool _ended = false;

    /// Call this when the driving test is finished
    public void EndRun()
    {
        if (_ended) return;
        _ended = true;

        Debug.Log("[EndRun] Ending run");

        // 1) Save data
        if (StudySessionManager.Instance != null)
            StudySessionManager.Instance.EndRunAndSave();

        // 2) Load Good Job scene
        SceneManager.LoadScene(goodJobSceneName);

        // foreach (var hub in FindObjectsOfType<CarInputHub>(true))
        //     hub.ShutdownInput();

        // 3) Quit after delay
        StartCoroutine(QuitAfterDelay());
    }

    public void OnTriggerEnter(Collider other)
    {
        EndRun();
    
    }

    private IEnumerator QuitAfterDelay()
    {
        yield return new WaitForSeconds(quitDelaySeconds);

#if UNITY_EDITOR
        Debug.Log("[EndRun] Exiting Play Mode");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Debug.Log("[EndRun] Quitting application");
        Application.Quit();
#endif
    }
}
