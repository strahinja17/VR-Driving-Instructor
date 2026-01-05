using UnityEngine;

public class InstructorGestureAnimator : MonoBehaviour
{
    public Animator animator;
    static readonly int ThumbsUp = Animator.StringToHash("ThumbsUp");

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

     public void DoThumbsUp()
    {
        if (animator == null) return;
        animator.ResetTrigger(ThumbsUp);
        animator.SetTrigger(ThumbsUp);
    }
}
