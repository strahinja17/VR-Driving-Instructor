using UnityEngine;

public class InstructorAnimationBundle : MonoBehaviour
{
    [Header("Sub-animations")]
    public InstructorHeadLook instructorHeadLook;
    public InstructorMouthFlap_JawBone mouthFlap;
    public InstructorGestureSimple gesture;

    [Header("Defaults")]
    public bool gestureOnStart = true;
    public GestureType defaultGesture = GestureType.PalmUp;

    public enum GestureType
    {
        None,
        PalmUp,
        ThumbsUp
    }

    private bool _talking;

    private void Awake()
    {
        // Auto-find if not wired (safe + convenient)
        if (instructorHeadLook == null)
            instructorHeadLook = GetComponentInChildren<InstructorHeadLook>(true);

        if (mouthFlap == null)
            mouthFlap = GetComponentInChildren<InstructorMouthFlap_JawBone>(true);

        if (gesture == null)
            gesture = GetComponentInChildren<InstructorGestureSimple>(true);
    }

    // =========================
    // PUBLIC API (THIS IS WHAT YOU USE)
    // =========================

    /// <summary>
    /// Call when instructor starts speaking.
    /// Used for AI directions, warnings, scripted VO, etc.
    /// </summary>
    public void BeginSpeech(GestureType gestureType = GestureType.None)
    {
        if (_talking) return;
        _talking = true;

        if (instructorHeadLook != null)
            instructorHeadLook.SetTalking(true);

        if (mouthFlap != null)
            mouthFlap.SetTalking(true);

        if (gestureOnStart)
            PlayGesture(gestureType == GestureType.None ? defaultGesture : gestureType);
    }

    /// <summary>
    /// Call when instructor finishes speaking.
    /// </summary>
    public void EndSpeech()
    {
        if (!_talking) return;
        _talking = false;

        if (instructorHeadLook != null)
            instructorHeadLook.SetTalking(false);

        if (mouthFlap != null)
            mouthFlap.SetTalking(false);
    }

    /// <summary>
    /// Trigger a gesture manually (without speech).
    /// </summary>
    public void PlayGesture(GestureType type)
    {
        if (gesture == null) return;

        switch (type)
        {
            case GestureType.PalmUp:
                gesture.DoPalmUp();
                break;

            case GestureType.ThumbsUp:
                gesture.DoThumbsUp();
                break;
        }
    }

    /// <summary>
    /// Utility: positive feedback bundle.
    /// </summary>
    public void Praise()
    {
        BeginSpeech(GestureType.ThumbsUp);
    }

    /// <summary>
    /// Utility: instruction / warning bundle.
    /// </summary>
    public void Instruct()
    {
        BeginSpeech(GestureType.PalmUp);
    }
}
