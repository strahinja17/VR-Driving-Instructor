using System;
using UnityEngine;

public class MicInputToInstructor : MonoBehaviour
{
    [Header("References")]
    public DrivingAIInstructorHub hub;

    [Header("Mic")]
    [Tooltip("Leave empty for default microphone.")]
    public string micDeviceName = "";
    public int micSampleRate = 48000;     // common on Windows; we will resample to 16000
    public int targetSampleRate = 16000;  // MUST match hub input_audio_format sample rate
    public int maxRecordSeconds = 10;

    [Header("Push-to-talk")]
    public KeyCode pushToTalkKey = KeyCode.V;

    [Tooltip("If true, auto-send when user releases key. If false, you must call SendNow().")]
    public bool sendOnRelease = true;

    [Header("Level gate")]
    [Tooltip("Skip sending if mic is basically silent.")]
    public float rmsSilenceThreshold = 0.01f;

    [Header("Prompt")]
    [TextArea(2, 5)]
    public string extraInstruction = "Answer the player's question in 1–2 short sentences.";

    private AudioClip _clip;
    private bool _recording;
    private int _startSamplePos;
    private string _device;

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
        }
        else
        {
            Debug.Log("[MicInputToInstructor] Using mic: " + _device);
        }
    }

    private void Update()
    {
        if (string.IsNullOrEmpty(_device) || hub == null) return;

        if (Input.GetKeyDown(pushToTalkKey))
            BeginRecording();

        if (Input.GetKeyUp(pushToTalkKey))
        {
            if (sendOnRelease)
                EndRecordingAndSend();
            else
                EndRecordingKeepBuffer();
        }
    }

    public void BeginRecording()
    {
        if (_recording) return;
        if (string.IsNullOrEmpty(_device)) return;

        // Create a looping clip so we can read from it reliably
        _clip = Microphone.Start(_device, loop: true, lengthSec: maxRecordSeconds, frequency: micSampleRate);
        _recording = true;

        // Wait a tiny bit for mic to start; sample position can be 0 initially
        _startSamplePos = 0;

        Debug.Log("[MicInputToInstructor] Recording started.");
    }

    public void EndRecordingKeepBuffer()
    {
        if (!_recording) return;
        _recording = false;

        int endPos = Microphone.GetPosition(_device);
        Microphone.End(_device);

        Debug.Log("[MicInputToInstructor] Recording ended (not sent). Samples: " + endPos);
        // You could store the audio here if you want manual send later.
    }

    public void EndRecordingAndSend()
    {
        if (!_recording) return;
        _recording = false;

        int endPos = Microphone.GetPosition(_device);
        Microphone.End(_device);

        if (_clip == null || endPos <= 0)
        {
            Debug.LogWarning("[MicInputToInstructor] No audio captured.");
            return;
        }

        // Pull samples out of the clip (mono)
        float[] samples = ReadSamples(_clip, endPos);

        if (samples == null || samples.Length < targetSampleRate / 10)
        {
            Debug.LogWarning("[MicInputToInstructor] Too little audio captured.");
            return;
        }

        // Silence gate
        float rms = ComputeRms(samples);
        if (rms < rmsSilenceThreshold)
        {
            Debug.Log("[MicInputToInstructor] Not sending: looks like silence (rms=" + rms.ToString("F4") + ")");
            return;
        }

        // Resample to 16k
        float[] resampled = ResampleLinear(samples, micSampleRate, targetSampleRate);

        // Convert to PCM16
        byte[] pcm16 = DrivingAIInstructorHub.ConvertFloatToPcm16(resampled);

        // Send to hub
        hub.NotifyPlayerVoiceOnly(pcm16, extraInstruction);

        Debug.Log($"[MicInputToInstructor] Sent mic audio. rawSamples={samples.Length} resampled={resampled.Length}");
    }

    private string ResolveDevice()
    {
        if (!string.IsNullOrEmpty(micDeviceName))
            return micDeviceName;

        if (Microphone.devices != null && Microphone.devices.Length > 0)
            return Microphone.devices[0];

        return null;
    }

    private float[] ReadSamples(AudioClip clip, int sampleCount)
    {
        try
        {
            // AudioClip.GetData expects length in samples * channels.
            int channels = clip.channels;
            float[] data = new float[sampleCount * channels];
            clip.GetData(data, 0);

            if (channels == 1) return data;

            // Downmix to mono
            float[] mono = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float sum = 0f;
                for (int ch = 0; ch < channels; ch++)
                    sum += data[i * channels + ch];
                mono[i] = sum / channels;
            }
            return mono;
        }
        catch (Exception e)
        {
            Debug.LogError("[MicInputToInstructor] ReadSamples failed: " + e);
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

    /// <summary>
    /// Simple linear resampler (good enough for voice).
    /// </summary>
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
