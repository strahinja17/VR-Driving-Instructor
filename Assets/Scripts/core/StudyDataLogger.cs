using System.IO;
using UnityEngine;

public static class StudyDataLogger
{
    private static string PathJsonl =>
        System.IO.Path.Combine(Application.persistentDataPath, "study_results.jsonl");

    public static void AppendJsonLine(StudyResult result)
    {
        try
        {
            string json = JsonUtility.ToJson(result);
            File.AppendAllText(PathJsonl, json + "\n");
            Debug.Log($"[StudyDataLogger] Saved to: {PathJsonl}\n{json}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StudyDataLogger] Save failed: {e}");
        }
    }
}
