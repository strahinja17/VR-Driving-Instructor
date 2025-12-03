using UnityEngine;

public class CarMirrors : MonoBehaviour
{
    [Header("Mirror Camera")]
    public Camera mirrorCamera;

    [Header("Viewpoints (on the car)")]
    public Transform viewCenter;
    public Transform viewLeft;
    public Transform viewRight;

    [Header("RenderTextures")]
    public RenderTexture rtCenter;
    public RenderTexture rtLeft;
    public RenderTexture rtRight;

    [Header("Performance")]
    [Tooltip("1 = every frame, 2 = every 2nd frame, etc.")]
    [Range(1, 5)]
    public int renderEveryNFrames = 2;

    private void Awake()
    {
        if (mirrorCamera != null)
        {
            // We render manually, so disable auto-render
            mirrorCamera.enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (mirrorCamera == null)
            return;

        if (Time.frameCount % renderEveryNFrames != 0)
            return;

        // Center mirror
        if (viewCenter != null && rtCenter != null)
            RenderMirror(viewCenter, rtCenter);

        // Left mirror
        if (viewLeft != null && rtLeft != null)
            RenderMirror(viewLeft, rtLeft);

        // Right mirror
        if (viewRight != null && rtRight != null)
            RenderMirror(viewRight, rtRight);
    }

    private void RenderMirror(Transform viewpoint, RenderTexture targetRT)
    {
        mirrorCamera.transform.position = viewpoint.position;
        mirrorCamera.transform.rotation = viewpoint.rotation;

        mirrorCamera.targetTexture = targetRT;
        mirrorCamera.Render();
    }
}
