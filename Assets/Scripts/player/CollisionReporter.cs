using System;
using UnityEngine;

public class CollisionReporter : MonoBehaviour
{
    [Header("Tags for collision types")]
    public string wallTag = "Wall";          // Curbs / barriers / walls
    public string aiCarTag = "AICar";        // Other vehicles
    public string pedestrianTag = "Pedestrian";

    [Header("Speed thresholds (m/s)")]
    [Tooltip("Below this speed, collisions are ignored.")]
    public float minSpeedToCare = 0.5f;

    [Tooltip("<= this speed = low severity")]
    public float lowSeverityMaxSpeed = 3f;

    [Tooltip("<= this speed = medium severity, above = high")]
    public float mediumSeverityMaxSpeed = 8f;

    private bool AIMode;

    public AudioClip collisionAudio;

    // You can listen to this from your metrics / AI instructor code
    [Serializable]
    public class CollisionEvent
    {
        public string objectType;   // "Wall", "AICar", "Pedestrian"
        public string severity;     // "Low", "Medium", "High"
        public float speed;         // impact speed in m/s
        public Vector3 position;    // world-space contact point
        public float time;          // Time.time
        public string message;      // human-readable description
    }

    public event Action<CollisionEvent> OnCollisionEvent;

    void Start()
    {
        AIMode = StudyConditionManager.Instance.IsAIEnabled;
    }

    private void OnCollisionEnter(Collision collision)
    {
        string tag = collision.collider.tag;

        string objectType = GetObjectTypeFromTag(tag);
        if (objectType == null)
            return; // Not one of the three types we care about

        float speed = collision.relativeVelocity.magnitude;
        if (speed < minSpeedToCare)
            return; // Ignore tiny bumps

        string severity = GetSeverityFromSpeed(speed);

        // First contact point is enough for logging
        Vector3 hitPoint = collision.GetContact(0).point;

        string message = BuildMessage(objectType, severity, speed);

        CollisionEvent ev = new CollisionEvent
        {
            objectType = objectType,
            severity = severity,
            speed = speed,
            position = hitPoint,
            time = Time.time,
            message = message
        };

        // Send to whoever is listening (your AI / recorder)
        OnCollisionEvent?.Invoke(ev);

        // Optional: debug log so you see it in the console
        Debug.Log($"[Collision] {message} (type={objectType}, severity={severity}, speed={speed:F2} m/s)");

        if (AIMode) {
        DrivingAIInstructorHub.Instance.NotifyDrivingEvent(
            eventName: "Collision",
            playerUtterance: null,
            extraInstruction: "The provided message describes the collision event briefly. This is a simulation, so don't react with compasion and offering emergency services, but rather react ot the severety, and object of collision and scold the player for driving too fast/carelessly"
                                + $": {message}");
        } 
            else
        {
            GlobalInstructorAudio.Play(collisionAudio);
        }
        
        StudySessionManager.Instance.RegisterWarning("Collision");
    }

    private string GetObjectTypeFromTag(string tag)
    {
        if (tag == wallTag) return "Wall";
        if (tag == aiCarTag) return "AICar";
        if (tag == pedestrianTag) return "Pedestrian";
        return null;
    }

    private string GetSeverityFromSpeed(float speed)
    {
        if (speed <= lowSeverityMaxSpeed) return "Low";
        if (speed <= mediumSeverityMaxSpeed) return "Medium";
        return "High";
    }

    private string BuildMessage(string objectType, string severity, float speed)
    {
        // You can tweak these texts however you like.
        switch (objectType)
        {
            case "Wall":
                switch (severity)
                {
                    case "Low":
                        return "You lightly touched the curb.";
                    case "Medium":
                        return "You hit the curb at moderate speed.";
                    case "High":
                        return "You crashed hard into the curb.";
                }
                break;

            case "AICar":
                switch (severity)
                {
                    case "Low":
                        return "You made light contact with another car.";
                    case "Medium":
                        return "You collided with another car.";
                    case "High":
                        return "You had a severe collision with another car.";
                }
                break;

            case "Pedestrian":
                switch (severity)
                {
                    case "Low":
                        return "You lightly bumped a pedestrian.";
                    case "Medium":
                        return "You hit a pedestrian.";
                    case "High":
                        return "You severely hit a pedestrian.";
                }
                break;
        }

        // Fallback (shouldn't really happen)
        return $"Collision with {objectType} (severity {severity}, speed {speed:F2} m/s).";
    }
}
