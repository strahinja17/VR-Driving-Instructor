using System;
using System.Collections.Generic;

[Serializable]
public class WarningPair
{
    public string reason;
    public int count;
}

[Serializable]
public class StudyResult
{
    public string nickname;
    public string mode;          // "AI" / "NoAI" / "TestAI"
    public string runId;
    public string startUtc;
    public string endUtc;

    // Sequential list of (reason,count) pairs
    public List<WarningPair> warnings;
}
