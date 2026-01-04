using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Central hub for your VR driving AI instructor, backed by the OpenAI Realtime API.
/// - Single realtime session with a driving-instructor system prompt.
/// - Other scripts call NotifyDrivingEvent(...) with event name + optional player text/audio.
/// - Streams back text and audio; exposes events for UI / audio.
/// - Includes rate limiting so events don't spam (e.g. lane excursion).
/// - Ensures all Unity-facing events fire on the Unity main thread.
/// </summary>
public class DrivingAIInstructorHub : MonoBehaviour
{
    public static DrivingAIInstructorHub Instance { get; private set; }

    [Header("OpenAI Realtime Settings")]
    [Tooltip("Your OpenAI API key (load securely in production).")]
    private string apiKey = LocalSecrets.API_KEY;

    [Tooltip("Realtime model, e.g. gpt-realtime or gpt-4o-realtime-preview")]
    [SerializeField] private string model = "gpt-realtime-mini";

    [Tooltip("Voice name, e.g. alloy, verse, ember")]
    [SerializeField] private string voice = "echo";

    [Header("Audio Settings")]
    [Tooltip("Sample rate of PCM16 audio you send as input (must match your mic capture).")]
    [SerializeField] private int inputSampleRate = 16000;

    [Tooltip("Sample rate of PCM16 audio you expect from the model (alloy is often 24000).")]
    [SerializeField] private int outputSampleRate = 24000;
    public int OutputSampleRate => outputSampleRate;

    [Header("Rate Limiting (seconds)")]
    [Tooltip("Minimum time between ANY instructor calls.")]
    [SerializeField] private float minSecondsBetweenAnyCalls = 10f;

    [Tooltip("Minimum time between calls of the SAME event name.")]
    [SerializeField] private float minSecondsBetweenSameEvent = 15f;

    [Header("Instructor System Prompt")]
    [TextArea(6, 12)]
    [SerializeField] private string baseSystemPrompt = @"
You are an in-car VR driving instructor in a training simulator.

Goals:
- Keep the driver safe and calm.
- Explain mistakes clearly but briefly.
- Adapt feedback to their current context.
- Prioritize safety-critical issues over minor optimizations.
- Adopt the persona of the driving instructor. Be more natural and unforced, NOT robotic.
- NEVER say things that an AI would say, like 'If you need further assistance let me know..', DON'T ANNOUNCE SENTENCES WITH 'WARNING'!
- Use the fact that you're an LLM and have the entire convo as context, when you see a pattern or something that can be inferred from the conversation, mention it briefly.

Rules:
- Respond in short 1 sentence bursts unless explicitly asked for a detailed explanation. This is a hard limit!! You can only speak for 5-8 seconds!!
- Speak in the first person to the player (reffer to player with 'you'), not third person.
- Never mention that you are an AI or a language model.
- If the situation indicates imminent danger, be firm and immediate.
- Directions are ABSOLUTE! NEVER MAKE UP DIRECTIONS OR DISTANCES, READ THEM FROM THE PROMPT VERBATIM!
- DON'T USE THE WORD 'WARNING' OR 'DIRECTION', WHEN ANOUNCING SOMETHING IN A SENTENCE! 

Input format:
You will receive messages that contain:
- GAME_EVENT: <eventName>
- If you recieve an audio input file, treat it as the player saying something. Repeat back the question, and answer. If unclear, ask the player to repeat.
- You may also receive raw audio input from the player: treat it as what they just said.
";

    [Header("Debug")]
    public bool logDebugMessages = true;

    // ----- Public events -----

    public event Action<string> OnInstructorTextDelta;
    public event Action<string> OnInstructorTextComplete;
    public event Action<byte[]> OnInstructorAudioChunk;
    public event Action OnInstructorAudioResponseComplete;
    public event Action<string> OnInstructorError;

    // ----- Internal state -----

    private ClientWebSocket _socket;
    private CancellationTokenSource _cts;
    private readonly byte[] _recvBuffer = new byte[64 * 1024];

    private bool _isConnected;
    private StringBuilder _currentTextResponse = new StringBuilder();

    // rate limiting
    private Dictionary<string, float> _lastEventTimeByName = new Dictionary<string, float>();
    private float _lastAnyEventTime = -999f;

    // main-thread dispatch
    private readonly object _mainThreadLock = new object();
    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    // ===== Request queue / single-flight =====

    private class PendingInstructorRequest
    {
        public string eventName;
        public string extraInstruction;
        public byte[] playerAudioPcm16;
        public float enqueuedAt;
    }

    [SerializeField] private int maxQueueSize = 25;
    [SerializeField] private bool dropOldestWhenFull = true;

    private readonly Queue<PendingInstructorRequest> _requestQueue = new Queue<PendingInstructorRequest>();
    private readonly object _queueLock = new object();

    private Task _queuePumpTask;
    private TaskCompletionSource<bool> _responseDoneTcs;
    private volatile bool _responseInFlight = false;

    // serialize socket sends
    private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);


    // ===== Unity lifecycle =====

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        _cts = new CancellationTokenSource();
        await ConnectAndInitializeAsync();
    }

    private void Update()
    {
        // Run any queued callbacks on the Unity main thread
        lock (_mainThreadLock)
        {
            while (_mainThreadActions.Count > 0)
            {
                var action = _mainThreadActions.Dequeue();
                action?.Invoke();
            }
        }
    }

    private async void OnDestroy()
    {
        try
        {
            _cts?.Cancel();
            if (_socket != null && _socket.State == WebSocketState.Open)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Shutting down", CancellationToken.None);
            }
        }
        catch (Exception e)
        {
            DebugLog("OnDestroy exception: " + e);
        }
    }

    private void EnqueueOnMainThread(Action action)
    {
        if (action == null) return;
        lock (_mainThreadLock)
        {
            _mainThreadActions.Enqueue(action);
        }
    }

    // ===== Connection + session setup =====

    private async Task ConnectAndInitializeAsync()
    {
        try
        {
            _socket = new ClientWebSocket();
            _socket.Options.SetRequestHeader("Authorization", $"Bearer {apiKey}");
            _socket.Options.SetRequestHeader("OpenAI-Beta", "realtime=v1");

            var uri = new Uri($"wss://api.openai.com/v1/realtime?model={model}");
            await _socket.ConnectAsync(uri, _cts.Token);
            _isConnected = _socket.State == WebSocketState.Open;

            DebugLog("Connected to Realtime API: " + _isConnected);

            // NOTE: no 'session.type' here anymore
            var sessionUpdate = new
            {
                type = "session.update",
                session = new
                {
                    instructions = baseSystemPrompt,
                    modalities = new[] { "text", "audio" },
                    input_audio_format = "pcm16",
                    output_audio_format = "pcm16",
                    voice = voice,
                    temperature = 0.2,
                    turn_detection = (object)null,
                    input_audio_transcription = new
                    {
                        model = "whisper-1"
                    }
                }
            };


            await SendJsonAsync(sessionUpdate);
            _ = Task.Run(ReceiveLoopAsync);
        }
        catch (Exception ex)
        {
            _isConnected = false;
            DebugLog("Failed to connect to Realtime API: " + ex);
            EnqueueOnMainThread(() =>
                OnInstructorError?.Invoke("Connection error: " + ex.Message));
        }
    }

    // ===== Public API =====

    public void NotifyDrivingEvent(
        string eventName,
        string extraInstruction = null,
        string playerUtterance = null,
        byte[] playerAudioPcm16 = null)
    {
        if (!_isConnected)
        {
            DebugLog("NotifyDrivingEvent called but not connected.");
            return;
        }

        // --- rate limiting (same as yours) ---
        float now = Time.unscaledTime;
        bool isDirection = eventName == "Directions";

        if (now - _lastAnyEventTime < minSecondsBetweenAnyCalls && !isDirection)
        {
            DebugLog($"Skipped event '{eventName}' due to global rate limit.");
            return;
        }

        if (_lastEventTimeByName.TryGetValue(eventName, out float lastTimeForThisEvent) && !isDirection)
        {
            if (now - lastTimeForThisEvent < minSecondsBetweenSameEvent)
            {
                DebugLog($"Skipped event '{eventName}' due to per-event rate limit.");
                return;
            }
        }

        _lastAnyEventTime = now;
        _lastEventTimeByName[eventName] = now;
        // --- end rate limiting ---

        // enqueue
        var req = new PendingInstructorRequest
        {
            eventName = eventName,
            extraInstruction = extraInstruction,
            playerAudioPcm16 = playerAudioPcm16,
            enqueuedAt = now
        };

        lock (_queueLock)
        {
            if (_requestQueue.Count >= maxQueueSize)
            {
                if (dropOldestWhenFull)
                {
                    _requestQueue.Dequeue(); // drop oldest
                }
                else
                {
                    DebugLog($"Dropped event '{eventName}' because queue is full.");
                    return;
                }
            }

            _requestQueue.Enqueue(req);
        }

        EnsureQueuePumpRunning();
    }

    private void EnsureQueuePumpRunning()
    {
        if (_queuePumpTask != null && !_queuePumpTask.IsCompleted) return;
        _queuePumpTask = Task.Run(PumpQueueAsync);
    }

    private async Task PumpQueueAsync()
    {
        while (_isConnected && _socket != null && _socket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
        {
            PendingInstructorRequest req = null;

            lock (_queueLock)
            {
                if (_requestQueue.Count > 0 && !_responseInFlight)
                    req = _requestQueue.Dequeue();
            }

            if (req == null)
            {
                await Task.Delay(10, _cts.Token);
                continue;
            }

            try
            {
                _responseInFlight = true;
                _responseDoneTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                await SendRequestAsync(req);

                // Wait until server says response is done (HandleServerEvent will complete the TCS)
                await _responseDoneTcs.Task;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                DebugLog("PumpQueueAsync error: " + ex);
                EnqueueOnMainThread(() => OnInstructorError?.Invoke("Queue pump error: " + ex.Message));
            }
            finally
            {
                _responseInFlight = false;
                _responseDoneTcs = null;
            }
        }
    }

    private async Task SendRequestAsync(PendingInstructorRequest req)
    {
        bool hasAudio = req.playerAudioPcm16 != null && req.playerAudioPcm16.Length > 0;
        bool isVoiceOnly = (req.eventName == "PlayerVoiceQuestion") && hasAudio;

        if (hasAudio)
            await SendAudioInputAsync(req.playerAudioPcm16);

        // Only create a text item if NOT voice-only
        if (!isVoiceOnly)
        {
            string payloadText =
                $"GAME_EVENT: {req.eventName}\n";
            
            await SendJsonAsync(new
            {
                type = "conversation.item.create",
                item = new
                {
                    type = "message",
                    role = "user",
                    content = new object[]
                    {
                        new { type = "input_text", text = payloadText }
                    }
                }
            });
        }

        await SendJsonAsync(new
        {
            type = "response.create",
            response = new
            {
                modalities = new[] { "text", "audio" },
                instructions =
                    (string.IsNullOrEmpty(req.extraInstruction) ? "" : req.extraInstruction + "\n") +
                    (isVoiceOnly
                        ? "Use the latest audio input as what the player just said. If unclear, ask them to repeat in one short sentence."
                        : "")
            }
        });

        _currentTextResponse.Clear();
    }


    // ===== Audio input to Realtime =====

    private async Task SendAudioInputAsync(byte[] pcm16Audio)
    {
        if (_socket == null || _socket.State != WebSocketState.Open) return;

        await SendJsonAsync(new { type = "input_audio_buffer.clear" });

        string base64 = Convert.ToBase64String(pcm16Audio);
        await SendJsonAsync(new { type = "input_audio_buffer.append", audio = base64 });

        await SendJsonAsync(new { type = "input_audio_buffer.commit" });
    }


    public static byte[] ConvertFloatToPcm16(float[] samples)
    {
        if (samples == null) return null;
        byte[] bytes = new byte[samples.Length * 2];
        int rescaleFactor = 32767;

        for (int i = 0; i < samples.Length; i++)
        {
            short s = (short)Mathf.Clamp(samples[i] * rescaleFactor, short.MinValue, short.MaxValue);
            bytes[2 * i] = (byte)(s & 0xFF);
            bytes[2 * i + 1] = (byte)((s >> 8) & 0xFF);
        }

        return bytes;
    }

    // ===== Sending & receiving =====

    private async Task SendJsonAsync(object payload)
    {
        if (_socket == null || _socket.State != WebSocketState.Open) return;

        string json = JsonConvert.SerializeObject(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        await _sendLock.WaitAsync(_cts.Token);
        try
        {
            await _socket.SendAsync(segment, WebSocketMessageType.Text, true, _cts.Token);
        }
        finally
        {
            _sendLock.Release();
        }

        if (logDebugMessages) DebugLog(">> " + json);
    }


    private async Task ReceiveLoopAsync()
    {
        DebugLog("Receive loop started.");

        while (_socket != null && _socket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
        {
            try
            {
                var builder = new StringBuilder();
                WebSocketReceiveResult result;

                do
                {
                    result = await _socket.ReceiveAsync(new ArraySegment<byte>(_recvBuffer), _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        DebugLog("WebSocket closed by server.");
                        _isConnected = false;
                        return;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var chunk = Encoding.UTF8.GetString(_recvBuffer, 0, result.Count);
                        builder.Append(chunk);
                    }
                }
                while (!result.EndOfMessage);

                string messageStr = builder.ToString();
                if (string.IsNullOrWhiteSpace(messageStr))
                    continue;

                if (logDebugMessages)
                {
                    DebugLog("<< " + messageStr);
                }

                HandleServerEvent(messageStr);
            }
            catch (OperationCanceledException)
            {
                DebugLog("Receive loop cancelled.");
                break;
            }
            catch (Exception ex)
            {
                DebugLog("Receive loop error: " + ex);
                EnqueueOnMainThread(() =>
                    OnInstructorError?.Invoke("Receive error: " + ex.Message));
                break;
            }
        }

        _isConnected = false;
        DebugLog("Receive loop ended.");
    }

    public void NotifyPlayerVoiceOnly(byte[] playerAudioPcm16,
        string extraInstruction = "Answer the player's question in 1–2 short sentences.")
    {
        if (!_isConnected)
        {
            DebugLog("NotifyPlayerVoiceOnly called but not connected.");
            return;
        }

        // Voice should not be rate-limited like violations. You can add a small cooldown if needed.
        var req = new PendingInstructorRequest
        {
            eventName = "PlayerVoiceQuestion",
            extraInstruction = extraInstruction,
            playerAudioPcm16 = playerAudioPcm16,
            enqueuedAt = Time.unscaledTime
        };

        lock (_queueLock)
        {
            if (_requestQueue.Count >= maxQueueSize)
            {
                if (dropOldestWhenFull) _requestQueue.Dequeue();
                else return;
            }
            _requestQueue.Enqueue(req);
        }

        EnsureQueuePumpRunning();
    }


    // ===== Handle server events =====

    private readonly StringBuilder _currentTranscript = new StringBuilder();
    public event Action<string> OnMicTranscript;

    private void HandleServerEvent(string messageStr)
    {
        JObject msg;
        try
        {
            msg = JObject.Parse(messageStr);
        }
        catch (Exception ex)
        {
            DebugLog("JSON parse error: " + ex + " | raw: " + messageStr);
            return;
        }

        string type = msg.Value<string>("type") ?? "";

        switch (type)
        {
            // Text streaming
            case "response.output_text.delta":
            case "response.text.delta":
            {
                string delta = msg.Value<string>("delta") ?? "";
                if (!string.IsNullOrEmpty(delta))
                {
                    _currentTextResponse.Append(delta);
                    EnqueueOnMainThread(() =>
                        OnInstructorTextDelta?.Invoke(delta));
                }
                break;
            }

            case "response.output_text.done":
            case "response.text.done":
            {
                string fullText = _currentTextResponse.ToString();
                EnqueueOnMainThread(() =>
                    OnInstructorTextComplete?.Invoke(fullText));
                break;
            }

            // Audio streaming
            case "response.output_audio.delta":
            case "response.audio.delta":
            {
                string base64 = msg.Value<string>("delta");
                if (!string.IsNullOrEmpty(base64))
                {
                    try
                    {
                        byte[] audioBytes = Convert.FromBase64String(base64);
                        EnqueueOnMainThread(() =>
                            OnInstructorAudioChunk?.Invoke(audioBytes));
                    }
                    catch (Exception ex)
                    {
                        DebugLog("Failed to decode audio delta: " + ex);
                    }
                }
                break;
            }

            case "response.output_audio.done":
            case "response.audio.done":
            {
                EnqueueOnMainThread(() =>
                    OnInstructorAudioResponseComplete?.Invoke());
                break;
            }

            case "response.done":
            {
                _responseDoneTcs?.TrySetResult(true);
                break;
            }

            case "error":
            {
                var errorObj = msg["error"];
                string errorMessage = errorObj?["message"]?.ToString() ?? "Unknown error";
                DebugLog("Realtime API error: " + errorMessage);
                EnqueueOnMainThread(() =>
                    OnInstructorError?.Invoke(errorMessage));
                break;
            }

            case "conversation.item.input_audio_transcription.delta":
            {
                // whisper-1 often sends whole transcript in delta too
                string delta = msg.Value<string>("delta") ?? "";
                if (!string.IsNullOrEmpty(delta))
                    DebugLog("[Mic transcript Δ] " + delta);
                break;
            }

            case "conversation.item.input_audio_transcription.completed":
            {
                string transcript = msg.Value<string>("transcript") ?? "";
                DebugLog("[Mic transcript DONE] " + transcript);
                break;
            }

            case "conversation.item.input_audio_transcription.failed":
            {
                // This is SUPER useful: often means your project doesn’t have access to the transcribe model
                DebugLog("[Mic transcript FAILED] " + msg.ToString(Newtonsoft.Json.Formatting.None));
                break;
            }




            default:
                break;
        }
    }

    // ===== Helper =====

    private void DebugLog(string msg)
    {
        if (logDebugMessages)
        {
            Debug.Log("[DrivingAIInstructorHub] " + msg);
        }
    }
}
