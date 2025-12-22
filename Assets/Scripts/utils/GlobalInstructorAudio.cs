using System.Collections.Generic;
using UnityEngine;

public class GlobalInstructorAudio : MonoBehaviour
{
    private static GlobalInstructorAudio _instance;

    [Header("Assign in Inspector")]
    public AudioSource audioSource;

    [Header("Playback")]
    [Tooltip("Minimum seconds between starting clips (prevents spam).")]
    public float minSecondsBetweenStarts = 1f;

    [Tooltip("If true, clips are queued and played sequentially.")]
    public bool useQueue = true;

    [Tooltip("Global volume multiplier.")]
    [Range(0f, 1f)] public float volume = 1f;

    // Internal queue
    private readonly Queue<AudioClip> _queue = new Queue<AudioClip>();

    private float _nextAllowedStartTime = 0f;
    private bool _isPlayingCoroutine;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D voice default
            }
        }
    }

    /// <summary>
    /// Enqueue or play an instructor audio clip globally.
    /// Never interrupts by default.
    /// </summary>
    public static void Play(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[GlobalInstructorAudio] Tried to play null clip.");
            return;
        }

        if (_instance == null)
        {
            Debug.LogError("[GlobalInstructorAudio] No instance in scene. Add GlobalInstructorAudio to a GameObject once.");
            return;
        }

        if (_instance.useQueue)
        {
            _instance._queue.Enqueue(clip);
            _instance.TryStartPump();
        }
        else
        {
            // "Hard no interrupt + cooldown" mode: only play if idle and cooldown passed
            _instance.TryPlayImmediate(clip);
        }
    }

    /// <summary>
    /// Stops audio and clears the queue.
    /// </summary>
    public static void StopAndClear()
    {
        if (_instance == null) return;
        _instance._queue.Clear();
        if (_instance.audioSource != null)
            _instance.audioSource.Stop();
        _instance._isPlayingCoroutine = false;
    }

    public static bool IsPlaying()
    {
        return _instance != null &&
               _instance.audioSource != null &&
               _instance.audioSource.isPlaying;
    }

    private void TryStartPump()
    {
        if (_isPlayingCoroutine) return;
        StartCoroutine(PumpQueue());
    }

    private void TryPlayImmediate(AudioClip clip)
    {
        if (audioSource == null) return;

        // Don't interrupt; only play if nothing currently playing
        if (audioSource.isPlaying) return;

        // Enforce global cooldown between starts
        if (Time.time < _nextAllowedStartTime) return;

        audioSource.volume = volume;
        audioSource.PlayOneShot(clip);

        _nextAllowedStartTime = Time.time + minSecondsBetweenStarts;
    }

    private System.Collections.IEnumerator PumpQueue()
    {
        _isPlayingCoroutine = true;

        while (true)
        {
            if (audioSource == null)
                break;

            // Wait until audio finishes if currently playing
            if (audioSource.isPlaying)
            {
                yield return null;
                continue;
            }

            // Nothing queued -> stop pumping
            if (_queue.Count == 0)
                break;

            // Enforce cooldown between clip starts
            if (Time.time < _nextAllowedStartTime)
            {
                yield return null;
                continue;
            }

            var next = _queue.Dequeue();

            audioSource.volume = volume;
            audioSource.PlayOneShot(next);

            // Cooldown starts now
            _nextAllowedStartTime = Time.time + minSecondsBetweenStarts;

            // Wait until finished (PlayOneShot sets isPlaying true during playback)
            yield return null;
        }

        _isPlayingCoroutine = false;
    }
}
