using UnityEngine;

public class WheelGrabController : MonoBehaviour
{
    [Header("References")]
    public WheelHandConstraint constraint;

    public Transform leftTracked;
    public Transform rightTracked;

    public HandVisualSnap leftVisual;
    public HandVisualSnap rightVisual;

    [Header("Grab state")]
    public bool leftGrabbing;
    public bool rightGrabbing;

    private void Awake()
    {
        if (constraint == null) constraint = GetComponent<WheelHandConstraint>();
    }

    private void LateUpdate()
    {
        if (constraint == null) return;

        // While grabbing, keep snap targets updated continuously (so hands follow rotation + user motion)
        if (leftGrabbing)
        {
            constraint.UpdateSnapFromHand(true, leftTracked);
            if (leftVisual != null) leftVisual.SnapTo(constraint.leftSnapTarget);
        }

        if (rightGrabbing)
        {
            constraint.UpdateSnapFromHand(false, rightTracked);
            if (rightVisual != null) rightVisual.SnapTo(constraint.rightSnapTarget);
        }
    }

    // Call these from your input system (pinch/grip)
    public void BeginGrabLeft()
    {
        leftGrabbing = true;
        if (leftVisual != null && constraint != null)
        {
            constraint.UpdateSnapFromHand(true, leftTracked);
            leftVisual.SnapTo(constraint.leftSnapTarget);
        }
    }

    public void EndGrabLeft()
    {
        leftGrabbing = false;
        leftVisual?.Unsnap();
    }

    public void BeginGrabRight()
    {
        rightGrabbing = true;
        if (rightVisual != null && constraint != null)
        {
            constraint.UpdateSnapFromHand(false, rightTracked);
            rightVisual.SnapTo(constraint.rightSnapTarget);
        }
    }

    public void EndGrabRight()
    {
        rightGrabbing = false;
        rightVisual?.Unsnap();
    }
}
