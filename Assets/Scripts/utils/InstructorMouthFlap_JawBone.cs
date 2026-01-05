using UnityEngine;

public class InstructorMouthFlap_JawBone : MonoBehaviour
{
    public Transform jawBone;                 // drag: jawroot (can be assigned later)

    [Header("Motion")]
    public float maxOpenDegrees = 10f;
    public float speed = 14f;
    public bool negativeZOpens = true;

    private Quaternion _baseLocal;
    private bool _hasBase;
    private bool _talking;
    private float _t;
    private bool _warned;

    public void SetTalking(bool talking)
    {
        _talking = talking;
    }

    private void LateUpdate()
    {
        if (jawBone == null)
        {
            if (!_warned)
            {
                _warned = true;
            }
            return;
        }

        // Cache base pose once (or recache if needed)
        if (!_hasBase)
        {
            _baseLocal = jawBone.localRotation;
            _hasBase = true;
        }

        float target = _talking ? Mathf.PerlinNoise(Time.time * 10f, 0f) : 0f;
        _t = Mathf.Lerp(_t, target, Time.deltaTime * speed);

        float angle = _t * maxOpenDegrees;
        if (negativeZOpens) angle = -angle;

        jawBone.localRotation = _baseLocal * Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
