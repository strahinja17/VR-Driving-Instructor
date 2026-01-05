using System.Collections;
using UnityEngine;

public class InstructorGestureSimple : MonoBehaviour
{
    public Transform rightUpperArm;
    public Transform rightForeArm;
    public Transform rightHand;

    public float raiseDuration = 0.25f;
    public float holdDuration = 0.6f;
    public float lowerDuration = 0.25f;

    private Quaternion _uaBase, _faBase, _handBase;
    private bool _busy;

    public InstructorGestureAnimator gestureAnimator;

    private void Start()
    {
        if (rightUpperArm) _uaBase = rightUpperArm.localRotation;
        if (rightForeArm) _faBase = rightForeArm.localRotation;
        if (rightHand) _handBase = rightHand.localRotation;
    }

    public void DoPalmUp()
    {
        if (_busy) return;
        gestureAnimator.DoThumbsUp();
    }

    public void DoThumbsUp()
    {
        if (_busy) return;
        StartCoroutine(GestureRoutine(
            uaTarget: _uaBase * Quaternion.Euler(-15f, 10f, 5f),
            faTarget: _faBase * Quaternion.Euler(-20f, 0f, 0f),
            handTarget: _handBase * Quaternion.Euler(0f, 0f, 20f)
        ));
    }

    private IEnumerator GestureRoutine(Quaternion uaTarget, Quaternion faTarget, Quaternion handTarget)
    {
        _busy = true;

        yield return TweenTo(raiseDuration, uaTarget, faTarget, handTarget);
        yield return new WaitForSeconds(holdDuration);
        yield return TweenTo(lowerDuration, _uaBase, _faBase, _handBase);

        _busy = false;
    }

    private IEnumerator TweenTo(float dur, Quaternion ua, Quaternion fa, Quaternion hand)
    {
        float t = 0f;
        Quaternion ua0 = rightUpperArm ? rightUpperArm.localRotation : Quaternion.identity;
        Quaternion fa0 = rightForeArm ? rightForeArm.localRotation : Quaternion.identity;
        Quaternion h0  = rightHand ? rightHand.localRotation : Quaternion.identity;

        while (t < dur)
        {
            t += Time.deltaTime;
            float a = dur <= 0f ? 1f : Mathf.Clamp01(t / dur);

            if (rightUpperArm) rightUpperArm.localRotation = Quaternion.Slerp(ua0, ua, a);
            if (rightForeArm)  rightForeArm.localRotation  = Quaternion.Slerp(fa0, fa, a);
            if (rightHand)     rightHand.localRotation     = Quaternion.Slerp(h0, hand, a);

            yield return null;
        }
    }
}
