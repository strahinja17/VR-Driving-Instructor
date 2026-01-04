using System.Collections;
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

    [Header("Anti-spam / dedupe")]
    [Tooltip("Minimum seconds before the SAME clip is allowed to be queued/played again.")]
    public float minTimeBetweenSameClip = 4f;

    [Tooltip("Hard cap to prevent runaway queue growth if something spams Play().")]
    public int maxQueueSize = 12;

    [Tooltip("If true, don't enqueue a clip if it's already waiting in the queue.")]
    public bool avoidDuplicatesAlreadyQueued = true;

    // Internal queue
    private readonly Queue<AudioClip> _queue = new Queue<AudioClip>();

    private float _nextAllowedStartTime = 0f;
    private Coroutine _pumpRoutine;

    // Track when each clip is next allowed to be ENQUEUED/PLAYED
    private readonly Dictionary<int, float> _nextAllowedTimeByClipId = new Dictionary<int, float>(64);

    // For quick duplicate checks without iterating the whole queue repeatedly
    private readonly HashSet<int> _queuedClipIds = new HashSet<int>();

    // Track what was last started (not just last requested)
    private int _lastStartedClipId = -1;

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
            Debug.LogError("[GlobalInstructorAudio] No instance in scene. Add GlobalInstructorAudio once.");
            return;
        }

        _instance.PlayInternal(clip);
    }

    private void PlayInternal(AudioClip clip)
    {
        if (audioSource == null) return;

        int id = clip.GetInstanceID();
        float now = Time.unscaledTime;

        // Anti-spam: per-clip cooldown for requests (applies to both queue and immediate)
        if (_nextAllowedTimeByClipId.TryGetValue(id, out float nextAllowed) && now < nextAllowed)
        {
            // Debug.Log($"[GlobalInstructorAudio] Ignored (clip cooldown): {clip.name}");
            return;
        }

        // Optional: don't enqueue if already queued
        if (useQueue && avoidDuplicatesAlreadyQueued && _queuedClipIds.Contains(id))
        {
            // Still update cooldown so callers spamming don't keep checking
            _nextAllowedTimeByClipId[id] = now + minTimeBetweenSameClip;
            return;
        }

        // Also avoid re-requesting the clip that just started very recently
        if (_lastStartedClipId == id && now < nextAllowed)
            return;

        // Reserve cooldown immediately (prevents per-frame queue spam)
        _nextAllowedTimeByClipId[id] = now + minTimeBetweenSameClip;

        if (useQueue)
        {
            // Cap queue size
            if (_queue.Count >= maxQueueSize)
            {
                // Drop newest request (or you could drop oldest; this is safest)
                // Debug.LogWarning("[GlobalInstructorAudio] Queue full, dropping clip: " + clip.name);
                return;
            }

            _queue.Enqueue(clip);
            _queuedClipIds.Add(id);
            TryStartPump();
        }
        else
        {
            TryPlayImmediate(clip);
        }
    }

    /// <summary>Stops audio and clears the queue.</summary>
    public static void StopAndClear()
    {
        if (_instance == null) return;

        _instance._queue.Clear();
        _instance._queuedClipIds.Clear();

        if (_instance.audioSource != null)
            _instance.audioSource.Stop();

        if (_instance._pumpRoutine != null)
        {
            _instance.StopCoroutine(_instance._pumpRoutine);
            _instance._pumpRoutine = null;
        }
    }

    public static bool IsPlaying()
    {
        return _instance != null &&
               _instance.audioSource != null &&
               _instance.audioSource.isPlaying;
    }

    private void TryStartPump()
    {
        if (_pumpRoutine != null) return;
        _pumpRoutine = StartCoroutine(PumpQueue());
    }

    private void TryPlayImmediate(AudioClip clip)
    {
        if (audioSource == null) return;

        // Don't interrupt; only play if nothing currently playing
        if (audioSource.isPlaying) return;

        // Enforce global cooldown between starts
        float now = Time.unscaledTime;
        if (now < _nextAllowedStartTime) return;

        StartClip(clip);
    }

    private void StartClip(AudioClip clip)
    {
        audioSource.volume = volume;
        audioSource.PlayOneShot(clip);

        _lastStartedClipId = clip.GetInstanceID();
        _nextAllowedStartTime = Time.unscaledTime + minSecondsBetweenStarts;
    }

    private IEnumerator PumpQueue()
    {
        while (true)
        {
            if (audioSource == null) break;

            if (audioSource.isPlaying)
            {
                yield return null;
                continue;
            }

            if (_queue.Count == 0) break;

            if (Time.unscaledTime < _nextAllowedStartTime)
            {
                yield return null;
                continue;
            }

            var next = _queue.Dequeue();
            _queuedClipIds.Remove(next.GetInstanceID());

            StartClip(next);

            // Let isPlaying flip properly
            yield return null;
        }

        _pumpRoutine = null;
    }
}
