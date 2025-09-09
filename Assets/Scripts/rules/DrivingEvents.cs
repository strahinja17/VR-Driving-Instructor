using System;

public static class DrivingEvents
{
    public static event Action<float,string> OnSpeedLimitChanged;   // (km/h, markerId)
    public static event Action<string>       OnStopLineCrossed;     // markerId
    public static event Action<string>       OnAdvisorySlowdown;    // markerId
    public static event Action<string>       OnScenarioStart;       // markerId
    public static event Action<string>       OnScenarioEnd;         // markerId

    public static void RaiseSpeedLimitChanged(float limit, string id) => OnSpeedLimitChanged?.Invoke(limit, id);
    public static void RaiseStopLineCrossed(string id)              => OnStopLineCrossed?.Invoke(id);
    public static void RaiseAdvisorySlowdown(string id)             => OnAdvisorySlowdown?.Invoke(id);
    public static void RaiseScenarioStart(string id)                => OnScenarioStart?.Invoke(id);
    public static void RaiseScenarioEnd(string id)                  => OnScenarioEnd?.Invoke(id);
}
