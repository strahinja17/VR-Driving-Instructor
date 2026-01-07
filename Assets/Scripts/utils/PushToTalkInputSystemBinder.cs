using UnityEngine;
using UnityEngine.InputSystem;

public class PushToTalkInputSystemBinder : MonoBehaviour
{
    [Header("References")]
    public MicInputToInstructor mic;

    [Tooltip("If not set, we will look for a PlayerInput on this GameObject or parent.")]
    public PlayerInput playerInput;

    [Tooltip("Action name in your DrivingControlls input actions. Example: \"Driving/PushToTalk\"")]
    public string pushToTalkActionName = "Driving/PushToTalk";

    private InputAction _pttAction;

    private void Awake()
    {
        if (mic == null) mic = GetComponent<MicInputToInstructor>();
        if (playerInput == null) playerInput = GetComponentInParent<PlayerInput>();

        if (mic == null)
        {
            Debug.LogError("[PTT Binder] MicInputToInstructor not assigned / found.");
            enabled = false;
            return;
        }

        if (playerInput == null)
        {
            Debug.LogError("[PTT Binder] No PlayerInput found. Add a PlayerInput component to your player rig (or assign it here).");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        // Find the action by name from the PlayerInput's actions asset
        _pttAction = playerInput.actions.FindAction(pushToTalkActionName, throwIfNotFound: false);

        if (_pttAction == null)
        {
            Debug.LogError($"[PTT Binder] Could not find action \"{pushToTalkActionName}\" in PlayerInput actions asset.");
            enabled = false;
            return;
        }

        _pttAction.started += OnPTTStarted;
        _pttAction.canceled += OnPTTCanceled;

        // Ensure enabled
        if (!_pttAction.enabled)
            _pttAction.Enable();
    }

    private void OnDisable()
    {
        if (_pttAction != null)
        {
            _pttAction.started -= OnPTTStarted;
            _pttAction.canceled -= OnPTTCanceled;
        }
    }

    private void OnPTTStarted(InputAction.CallbackContext ctx)
    {
        mic.BeginRecording();
    }

    private void OnPTTCanceled(InputAction.CallbackContext ctx)
    {
        if (mic.sendOnRelease)
            mic.EndRecordingAndSend();
        else
            mic.EndRecordingKeepBuffer();
    }
}
