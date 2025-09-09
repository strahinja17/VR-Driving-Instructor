using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class RuleSystem : MonoBehaviour
{
    public float currentSpeedLimitKmh = 50f;
    Vector3 lastPos;

    void Start() => lastPos = transform.position;

    void Update()
    {
        Vector3 now = transform.position;

        foreach (var lm in FindObjectsOfType<LineMarker>())
        {
            var plane = lm.GetPlane();
            float da = plane.GetDistanceToPoint(lastPos);
            float db = plane.GetDistanceToPoint(now);

            bool crossed = (lm.oneWay && da > 0f && db <= 0f) || (!lm.oneWay && Mathf.Sign(da) != Mathf.Sign(db));
            if (!crossed) continue;

            switch (lm.type)
            {
                case LineType.SpeedLimit:
                    currentSpeedLimitKmh = lm.speedLimitKmh;
                    DrivingEvents.RaiseSpeedLimitChanged(lm.speedLimitKmh, lm.markerId);
                    Debug.Log($"[RuleSystem] Speed limit now {lm.speedLimitKmh} via {lm.markerId}");
                    break;

                case LineType.StopLine:
                    DrivingEvents.RaiseStopLineCrossed(lm.markerId);
                    Debug.Log($"[RuleSystem] Stop line crossed ({lm.markerId})");
                    break;
            }
        }

        lastPos = now;
    }
}
