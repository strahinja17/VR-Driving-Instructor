using UnityEngine;

public enum StudyMode
{
    NoAI = 0,
    AI = 1,
    TestAI = 2
}


public class StudyConditionManager : MonoBehaviour
{
    public static StudyConditionManager Instance { get; private set; }

    [Header("Mode")]
    public StudyMode mode = StudyMode.AI;

    public bool IsAIEnabled => mode == StudyMode.AI || mode == StudyMode.TestAI;
    public bool ShouldCollectData => mode != StudyMode.TestAI;

    [Header("LLM Hub (optional)")]
    [Tooltip("Drag your DrivingAIInstructorHub component here.")]
    public Behaviour llmInstructorHub;

    [Header("Participant")]
    public string nickname = "";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetMode(StudySessionManager.Instance.mode);
    }

    public void SetMode(StudyMode newMode)
    {
        mode = newMode;
        ApplyMode();
        Debug.Log($"[StudyConditionManager] Mode set to: {mode}");
    }

    public void ToggleMode()
    {
        SetMode(mode == StudyMode.AI ? StudyMode.NoAI : StudyMode.AI);
    }

    private void ApplyMode()
    {
        if (llmInstructorHub != null)
            llmInstructorHub.enabled = (mode == StudyMode.AI);
    }
}
