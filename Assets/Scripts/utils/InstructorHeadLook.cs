using UnityEngine;

public class InstructorHeadLook : MonoBehaviour
{
    [Header("Bone")]
    public Transform headBone;   // drag: head

    [Header("Rotation Offset (degrees)")]
    public float offsetX = 50f;  // pitch
    public float offsetY = 0f;   // usually leave 0
    public float offsetZ = -10f; // roll/yaw-ish for your rig

    [Header("Motion")]
    public float blendSpeed = 6f;

    private Quaternion _baseLocal;
    private Quaternion _targetLocal;
    private bool _hasBase;
    private bool _talking;

    private void Start()
    {
        if (headBone == null)
        {
            enabled = false;
            return;
        }

        CacheBase();
    }

    /// <summary>Call this once when speech starts.</summary>
    public void SetTalking(bool talking)
    {
        _talking = talking;

        if (_talking)
        {
            // Offset from ORIGINAL pose, not current
            _targetLocal = _baseLocal * Quaternion.Euler(offsetX, offsetY, offsetZ);
        }
    }

    /// <summary>Call this if you ever want to recapture neutral pose.</summary>
    public void CacheBase()
    {
        _baseLocal = headBone.localRotation;
        _hasBase = true;
    }

    private void LateUpdate()
    {
        if (!_hasBase) return;

        Quaternion desired = _talking ? _targetLocal : _baseLocal;

        headBone.localRotation = Quaternion.Slerp(
            headBone.localRotation,
            desired,
            Time.deltaTime * blendSpeed
        );
    }
}
