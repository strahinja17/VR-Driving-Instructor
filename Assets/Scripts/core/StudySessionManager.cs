using System;
using System.Collections.Generic;
using UnityEngine;

public class StudySessionManager : MonoBehaviour
{
    public static StudySessionManager Instance { get; private set; }

    public string runId { get; private set; }
    public DateTime startUtc { get; private set; }

    public string nickname;

    [Header("Mode")]
    public StudyMode mode = StudyMode.AI;

    // Reason -> total count
    private readonly Dictionary<string, int> _warningCounts =
        new Dictionary<string, int>(64);

    // Reason -> last time it was counted (Time.unscaledTime)
    private readonly Dictionary<string, float> _lastWarningTime =
        new Dictionary<string, float>(64);

    [SerializeField]
    private float warningCooldownSeconds = 12f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// Call this once when the “drive/test” begins (each time you run the scenario).
    public void BeginRun()
    {
        _warningCounts.Clear();
        _lastWarningTime.Clear();
        runId = Guid.NewGuid().ToString("N");
        startUtc = DateTime.UtcNow;

        Debug.Log($"[StudySession] BeginRun runId={runId}, mode={StudyConditionManager.Instance?.mode}, nick={StudyConditionManager.Instance?.nickname}");
    }

    public void SetMode(StudyMode newMode)
    {
        mode = newMode;
    }

    public void RegisterWarning(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            reason = "Unknown";

        float now = Time.unscaledTime;

        // Cooldown check
        if (_lastWarningTime.TryGetValue(reason, out float lastTime))
        {
            if (now - lastTime < warningCooldownSeconds)
            {
                // Still in cooldown → ignore
                Debug.Log($"[StudySession] Warning '{reason}' ignored (cooldown)");
                return;
            }
        }

        // Passed cooldown → count it
        _lastWarningTime[reason] = now;

        if (_warningCounts.TryGetValue(reason, out int current))
            _warningCounts[reason] = current + 1;
        else
            _warningCounts[reason] = 1;

        Debug.Log($"[StudySession] Warning '{reason}' counted => {_warningCounts[reason]}");
    }


    /// Call this once when the run ends (pass/fail/timeout).
    public void EndRunAndSave()
    {
        var scm = StudyConditionManager.Instance;
        if (scm == null)
        {
            Debug.LogWarning("[StudySession] No StudyConditionManager found; cannot save.");
            return;
        }

        // If you still want TestAI to NOT save, keep this guard:
        // (Remove this if you want TestAI saved too.)
        if (scm.mode == StudyMode.TestAI)
        {
            Debug.Log("[StudySession] TestAI mode: not saving.");
            return;
        }

        var warningsList = new List<WarningPair>(_warningCounts.Count);
        foreach (var kvp in _warningCounts)
        {
            warningsList.Add(new WarningPair
            {
                reason = kvp.Key,
                count = kvp.Value
            });
        }

        // Optional: stable ordering (nice for analysis diffs)
        warningsList.Sort((a, b) => string.Compare(a.reason, b.reason, StringComparison.Ordinal));

        var result = new StudyResult
        {
            nickname = nickname,
            mode = mode.ToString(), // THIS is your “value added to json based on the mode”
            runId = runId,
            startUtc = startUtc.ToString("o"),
            endUtc = DateTime.UtcNow.ToString("o"),
            warnings = warningsList
        };

        StudyDataLogger.AppendJsonLine(result);
    }
}
