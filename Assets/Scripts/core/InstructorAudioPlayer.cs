using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Streams AI instructor audio from DrivingAIInstructorHub into an AudioSource in realtime,
/// resampling from 24kHz (model output) to Unity's output sample rate.
/// Uses a ring buffer and OnAudioFilterRead to avoid big spikes and lag.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class InstructorAudioPlayer : MonoBehaviour
{
    [Tooltip("How many seconds of instructor audio to buffer in memory.")]
    [SerializeField] private int bufferLengthSeconds = 10;

    // OpenAI Realtime pcm16 output is 24kHz mono
    private const int SourceSampleRate = 24000;

    public AudioSource _audioSource;

    // Ring buffer storing OUTPUT-rate float samples (mono)
    private float[] _ringBuffer;
    private int _ringBufferSize;   // in samples
    private int _writeIndex;
    private int _readIndex;
    private int _bufferedSamples;

    private readonly object _lockObj = new object();

    private int _outputSampleRate;
    private bool _isInitialized;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = true; // keeps OnAudioFilterRead being called
    }

    private void Start()
    {
        // Unity's actual output sample rate (usually 48000 on desktop)
        _outputSampleRate = AudioSettings.outputSampleRate;
        if (_outputSampleRate <= 0)
        {
            _outputSampleRate = 48000; // fallback
        }

        _ringBufferSize = Mathf.Max(_outputSampleRate * bufferLengthSeconds, _outputSampleRate);
        _ringBuffer = new float[_ringBufferSize];
        _writeIndex = 0;
        _readIndex = 0;
        _bufferedSamples = 0;

        // Create a dummy looping clip so the AudioSource keeps asking for data
        AudioClip clip = AudioClip.Create(
            "InstructorStreamSilence",
            _outputSampleRate,  // 1 second
            1,
            _outputSampleRate,
            false
        );

        float[] silence = new float[_outputSampleRate];
        clip.SetData(silence, 0);

        _audioSource.clip = clip;
        _audioSource.Play();

        _isInitialized = true;
    }

    private Coroutine _subCoroutine;

    private void OnEnable()
    {
        _subCoroutine = StartCoroutine(WaitAndSubscribe());
    }

    private void OnDisable()
    {
        if (_subCoroutine != null) StopCoroutine(_subCoroutine);
        Unsubscribe();
    }

    private System.Collections.IEnumerator WaitAndSubscribe()
    {
        // wait until hub instance exists
        while (DrivingAIInstructorHub.Instance == null)
            yield return null;

        Subscribe();
    }

    private void Subscribe()
    {
        DrivingAIInstructorHub.Instance.OnInstructorAudioChunk += HandleAudioChunk;
    }

    private void Unsubscribe()
    {
        if (DrivingAIInstructorHub.Instance == null) return;
        DrivingAIInstructorHub.Instance.OnInstructorAudioChunk -= HandleAudioChunk;
    }


    /// <summary>
    /// Called on main thread when the hub receives a new PCM16 chunk from the model.
    /// We convert from PCM16 @ 24kHz -> float[] @ Unity output rate (e.g. 48kHz) and push into ring buffer.
    /// </summary>
    private void HandleAudioChunk(byte[] chunk)
    {
        if (!_isInitialized || chunk == null || chunk.Length < 2) return;

        int sourceSampleCount = chunk.Length / 2;
        if (sourceSampleCount <= 0) return;

        // Convert PCM16 -> float[] at source sample rate (24k)
        float[] source = new float[sourceSampleCount];
        for (int i = 0; i < sourceSampleCount; i++)
        {
            short sample = (short)(chunk[2 * i] | (chunk[2 * i + 1] << 8));
            source[i] = sample / 32768f;
        }

        // Resample to outputSampleRate using simple linear interpolation
        float ratio = (float)_outputSampleRate / SourceSampleRate;
        int targetSampleCount = Mathf.CeilToInt(sourceSampleCount * ratio);

        lock (_lockObj)
        {
            for (int t = 0; t < targetSampleCount; t++)
            {
                float srcPos = t / ratio;                 // fractional index in source
                int i0 = Mathf.FloorToInt(srcPos);
                int i1 = Mathf.Min(i0 + 1, sourceSampleCount - 1);
                float frac = srcPos - i0;

                float s = source[i0] * (1f - frac) + source[i1] * frac;

                _ringBuffer[_writeIndex] = s;
                _writeIndex = (_writeIndex + 1) % _ringBufferSize;

                if (_bufferedSamples < _ringBufferSize)
                {
                    _bufferedSamples++;
                }
                else
                {
                    // Buffer full: overwrite oldest sample
                    _readIndex = (_readIndex + 1) % _ringBufferSize;
                }
            }
        }
    }

    /// <summary>
    /// Audio thread callback — Unity asks for 'data.Length / channels' frames at the output sample rate.
    /// We feed it from our ring buffer; if we run out, we output silence.
    /// </summary>
    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (!_isInitialized || channels <= 0) return;

        int frames = data.Length / channels;

        lock (_lockObj)
        {
            for (int frame = 0; frame < frames; frame++)
            {
                float sample = 0f;

                if (_bufferedSamples > 0)
                {
                    sample = _ringBuffer[_readIndex];
                    _readIndex = (_readIndex + 1) % _ringBufferSize;
                    _bufferedSamples--;
                }
                else
                {
                    // No data available: silence
                    sample = 0f;
                }

                // Copy mono sample to all channels (L, R, etc.)
                for (int ch = 0; ch < channels; ch++)
                {
                    data[frame * channels + ch] = sample;
                }
            }
        }
    }
}
