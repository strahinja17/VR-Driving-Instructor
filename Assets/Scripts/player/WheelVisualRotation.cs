using UnityEngine;

public class WheelVisualRotation : MonoBehaviour
{
    [System.Serializable]
    public class WheelGroup
    {
        public Transform[] wheelMeshes; // all LOD meshes
    }

    public WheelGroup[] wheels;
    public float wheelRadius = 0.34f; 
    public Rigidbody carRb;

    void Update()
    {
        // Signed speed: forward = positive, reverse = negative
        float signedSpeed = Vector3.Dot(carRb.linearVelocity, transform.forward);

        // Degrees per second
        float rotationSpeed = (signedSpeed / wheelRadius) * Mathf.Rad2Deg;

        foreach (var wheel in wheels)
        {
            foreach (var mesh in wheel.wheelMeshes)
            {
                if (mesh)
                {
                    mesh.Rotate(rotationSpeed * Time.deltaTime, 0f, 0f, Space.Self);
                }
            }
        }
    }
}
