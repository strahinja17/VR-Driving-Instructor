using UnityEngine;

public enum StudyMode
{
    NoAI = 0,
    AI = 1
}

public class StudyConditionManager : MonoBehaviour
{
    public static StudyConditionManager Instance { get; private set; }

    [Header("Mode")]
    public StudyMode mode = StudyMode.AI;

    [Header("LLM Hub (optional)")]
    [Tooltip("Drag your DrivingAIInstructorHub component here.")]
    public Behaviour llmInstructorHub;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyMode();
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

    public bool IsAIEnabled => mode == StudyMode.AI;

    private void ApplyMode()
    {
        if (llmInstructorHub != null)
            llmInstructorHub.enabled = (mode == StudyMode.AI);
    }
}
