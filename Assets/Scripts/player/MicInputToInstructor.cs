using System;
using UnityEngine;

public class MicInputToInstructor : MonoBehaviour
{
    [Header("References")]
    public DrivingAIInstructorHub hub;

    [Header("Mic")]
    [Tooltip("Leave empty for default microphone.")]
    public string micDeviceName = "";

    [Tooltip("Use 16000 if possible to avoid resampling.")]
    public int micSampleRate = 16000;

    public int targetSampleRate = 16000;
    public int maxRecordSeconds = 10;

    [Header("Push-to-talk")]
    public KeyCode pushToTalkKey = KeyCode.V;

    [Tooltip("If true, auto-send when user releases key.")]
    public bool sendOnRelease = true;

    [Header("Send window")]
    [Range(0.5f, 8f)] public float sendLastSeconds = 3.5f;

    [Header("Level gate")]
    public float rmsSilenceThreshold = 0.01f;

    [Header("Prompt")]
    [TextArea(2, 5)]
    public string extraInstruction = "Answer the player's question in 1–2 short sentences.";

    private AudioClip _clip;
    private bool _recording;
    private string _device;

    [Header("Optional: CarInputHub PTT")]
    public CarInputHub inputHub;

    private bool _prevPTTHeld;

    private void Awake()
    {
        if (hub == null)
            hub = FindObjectOfType<DrivingAIInstructorHub>();
    }

    private void Start()
    {
        _device = ResolveDevice();
        if (string.IsNullOrEmpty(_device))
        {
            Debug.LogWarning("[MicInputToInstructor] No microphone found.");
            return;
        }

        Debug.Log("[MicInputToInstructor] Using mic: " + _device);

        // START MIC ONCE (prevents press hitch)
        _clip = Microphone.Start(_device, loop: true, lengthSec: maxRecordSeconds, frequency: micSampleRate);

        Debug.Log("[MicInputToInstructor] Mic started (always-on).");
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(_device))
            Microphone.End(_device);
    }

    private void Update()
    {
        if (string.IsNullOrEmpty(_device) || hub == null || _clip == null) return;

        bool wheelHeld = (inputHub != null) && inputHub.PushToTalkHeld;
        bool keyHeld = Input.GetKey(pushToTalkKey);
        bool heldNow = wheelHeld || keyHeld;

        if (heldNow)
        {
            _recording = true;
            // (optional) Debug.Log("[MicInputToInstructor] PTT down");
        }

        if (_recording && !heldNow)
        {
            if (sendOnRelease)
                EndRecordingAndSend_FromRunningMic();
            _recording = false;
        }
    }

    private void EndRecordingAndSend_FromRunningMic()
    {
        int endPos = Microphone.GetPosition(_device);
        if (endPos <= 0)
        {
            Debug.LogWarning("[MicInputToInstructor] Mic position is 0.");
            return;
        }

        int freq = _clip.frequency;
        int channels = _clip.channels;

        int sendSamples = Mathf.Clamp(
            Mathf.RoundToInt(sendLastSeconds * freq),
            1,
            _clip.samples
        );

        // Read last window from looping buffer
        float[] mono = ReadLastSamplesMono(_clip, endPos, sendSamples, channels);
        if (mono == null || mono.Length < targetSampleRate / 10)
        {
            Debug.LogWarning("[MicInputToInstructor] Too little audio captured.");
            return;
        }

        float rms = ComputeRms(mono);
        if (rms < rmsSilenceThreshold)
        {
            Debug.Log($"[MicInputToInstructor] Not sending: silence (rms={rms:F4})");
            return;
        }

        // Resample only if needed
        float[] resampled = (freq == targetSampleRate) ? mono : ResampleLinear(mono, freq, targetSampleRate);

        byte[] pcm16 = DrivingAIInstructorHub.ConvertFloatToPcm16(resampled);
        hub.NotifyPlayerVoiceOnly(pcm16, extraInstruction);

        Debug.Log($"[MicInputToInstructor] Sent last {sendLastSeconds:F1}s (freq {freq} -> {targetSampleRate})");
    }

    private string ResolveDevice()
    {
        if (!string.IsNullOrEmpty(micDeviceName))
            return micDeviceName;

        if (Microphone.devices != null && Microphone.devices.Length > 0)
            return Microphone.devices[0];

        return null;
    }

    /// <summary>
    /// Reads the LAST 'count' samples ending at endPos from a looping AudioClip and returns mono.
    /// endPos is in samples per channel.
    /// </summary>
    private float[] ReadLastSamplesMono(AudioClip clip, int endPos, int count, int channels)
    {
        try
        {
            int bufferLen = clip.samples; // per channel
            count = Mathf.Min(count, bufferLen);

            int startPos = endPos - count;
            if (startPos < 0) startPos += bufferLen;

            float[] interleaved = new float[count * channels];

            if (startPos + count <= bufferLen)
            {
                clip.GetData(interleaved, startPos);
            }
            else
            {
                int tailCount = bufferLen - startPos;
                float[] tail = new float[tailCount * channels];
                float[] head = new float[(count - tailCount) * channels];

                clip.GetData(tail, startPos);
                clip.GetData(head, 0);

                Buffer.BlockCopy(tail, 0, interleaved, 0, tail.Length * sizeof(float));
                Buffer.BlockCopy(head, 0, interleaved, tail.Length * sizeof(float), head.Length * sizeof(float));
            }

            if (channels == 1) return interleaved;

            float[] mono = new float[count];
            for (int i = 0; i < count; i++)
            {
                float sum = 0f;
                int baseIdx = i * channels;
                for (int ch = 0; ch < channels; ch++)
                    sum += interleaved[baseIdx + ch];
                mono[i] = sum / channels;
            }
            return mono;
        }
        catch (Exception e)
        {
            Debug.LogError("[MicInputToInstructor] ReadLastSamplesMono failed: " + e);
            return null;
        }
    }

    private static float ComputeRms(float[] samples)
    {
        double sum = 0;
        for (int i = 0; i < samples.Length; i++)
            sum += samples[i] * samples[i];
        return (float)Math.Sqrt(sum / Math.Max(1, samples.Length));
    }

    private static float[] ResampleLinear(float[] input, int inRate, int outRate)
    {
        if (inRate == outRate) return input;

        double ratio = (double)outRate / inRate;
        int outLen = (int)Math.Ceiling(input.Length * ratio);
        float[] output = new float[outLen];

        for (int i = 0; i < outLen; i++)
        {
            double srcIndex = i / ratio;
            int i0 = (int)Math.Floor(srcIndex);
            int i1 = Math.Min(i0 + 1, input.Length - 1);
            double t = srcIndex - i0;

            float s0 = input[Mathf.Clamp(i0, 0, input.Length - 1)];
            float s1 = input[Mathf.Clamp(i1, 0, input.Length - 1)];
            output[i] = (float)((1.0 - t) * s0 + t * s1);
        }

        return output;
    }
}
