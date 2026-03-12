// PÅKREVD: MQTTnet.dll i Assets/Plugins/
// Meta Quest på device: sett brokerHost til PC-ens lokale IP (f.eks. 192.168.x.x)
// Unity Editor: localhost fungerer direkte

using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using UnityEngine;

/// <summary>
/// Singleton MQTT-publisher som bruker MQTTnet 4.x.
/// Events queues fra Unity main thread og publiseres via async Task i bakgrunnen.
/// Automatisk reconnect hvis broker faller ned.
/// Legg GameObject med denne komponenten i første scene – DontDestroyOnLoad holder den aktiv.
/// </summary>
public class MqttService : MonoBehaviour
{
    public static MqttService Instance { get; private set; }

    [Header("Broker")]
    [SerializeField] private string brokerHost = "157.245.79.217";
    [SerializeField] private int    brokerPort  = 1883;
    [SerializeField] private string clientId    = "traftec-vr";

    [Header("Topics")]
    [SerializeField] private string topicEvents       = "training/events";
    [SerializeField] private string topicSession      = "training/session";
    [SerializeField] private string topicHse          = "training/hse";
    [SerializeField] private string topicScoreResponse = "training/score/response";

    /// <summary>
    /// Fired on the Unity main thread when the backend publishes a score response.
    /// Subscribe in UI components to display the authoritative final score.
    /// </summary>
    public static event Action<string, int> OnScoreResponseReceived;

    // [IMPROVEMENT 3] Bounded publish queue – max 1000 events.
    private const int MaxQueueSize = 1000;

    private IMqttClient _client;
    private MqttClientOptions _options;
    private readonly ConcurrentQueue<(string topic, string payload)> _queue        = new();
    private readonly ConcurrentQueue<string>                         _scoreRawQueue = new();
    private CancellationTokenSource _cts;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _cts = new CancellationTokenSource();
        Task.Run(() => RunAsync(_cts.Token));
    }

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private void OnApplicationQuit()
    {
        _cts?.Cancel();
    }

    // ── Async backend (kjører utenfor Unity main thread) ─────────────────────────

    private async Task RunAsync(CancellationToken ct)
    {
        var factory = new MqttFactory();
        _client = factory.CreateMqttClient();

        // [IMPROVEMENT 1] Unique client ID: hardware device ID with GUID fallback so
        // multiple Unity clients or restarts cannot collide on the broker.
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        if (string.IsNullOrEmpty(deviceId) || deviceId == SystemInfo.unsupportedIdentifier)
            deviceId = Guid.NewGuid().ToString("N");
        string uid = clientId + "-" + deviceId;

        _options = new MqttClientOptionsBuilder()
            .WithTcpServer(brokerHost, brokerPort)
            .WithClientId(uid)
            .WithCleanSession()
            .Build();

        // [IMPROVEMENT 2] Disconnect/reconnect logging.
        _client.DisconnectedAsync += async e =>
        {
            if (ct.IsCancellationRequested) return;
            Debug.LogWarning("[MQTT] Disconnected. Retrying in 5 seconds...");
            await Task.Delay(5000).ConfigureAwait(false);
            if (!ct.IsCancellationRequested)
            {
                Debug.Log("[MQTT] Reconnecting...");
                await TryConnectAsync(ct).ConfigureAwait(false);
            }
        };

        // Route inbound messages from subscribed topics to the main-thread queue.
        _client.ApplicationMessageReceivedAsync += e =>
        {
            if (e.ApplicationMessage.Topic == topicScoreResponse)
            {
                var seg = e.ApplicationMessage.PayloadSegment;
                string raw = seg.Array != null
                    ? Encoding.UTF8.GetString(seg.Array, seg.Offset, seg.Count)
                    : string.Empty;
                if (!string.IsNullOrEmpty(raw))
                    _scoreRawQueue.Enqueue(raw);
            }
            return Task.CompletedTask;
        };

        await TryConnectAsync(ct).ConfigureAwait(false);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                bool published = false;
                while (_client.IsConnected)
                {
                    if (!_queue.TryDequeue(out var item)) break;

                    var msg = new MqttApplicationMessageBuilder()
                        .WithTopic(item.topic)
                        .WithPayload(Encoding.UTF8.GetBytes(item.payload))
                        .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();

                    await _client.PublishAsync(msg, ct).ConfigureAwait(false);
                    published = true;
                }

                if (!published)
                    await Task.Delay(50).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MQTT] Publish failed: {e.Message}. Retrying...");
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        if (_client?.IsConnected == true)
        {
            try { await _client.DisconnectAsync().ConfigureAwait(false); } catch { }
        }
    }

    private async Task TryConnectAsync(CancellationToken ct)
    {
        try
        {
            // [IMPROVEMENT 2] Log connection attempt before and result after.
            Debug.Log($"[MQTT] Connecting to {brokerHost}:{brokerPort}...");
            await _client.ConnectAsync(_options, ct).ConfigureAwait(false);
            Debug.Log("[MQTT] Connected.");

            // Subscribe to the score-response topic after every successful connection
            // (re-subscribe is required after reconnects with CleanSession=true).
            var subOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(topicScoreResponse, MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            await _client.SubscribeAsync(subOptions, ct).ConfigureAwait(false);
            Debug.Log($"[MQTT] Subscribed to {topicScoreResponse}");
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Debug.LogWarning($"[MQTT] Connection failed: {e.Message}. Events will be buffered locally.");
        }
    }

    // ── Offentlig API (kalles fra Unity main thread) ─────────────────────────────

    /// <summary>
    /// Drains inbound score-response messages on the Unity main thread and fires
    /// <see cref="OnScoreResponseReceived"/> so UI components can react without
    /// needing thread-marshalling themselves.
    /// </summary>
    private void Update()
    {
        while (_scoreRawQueue.TryDequeue(out string raw))
        {
            try
            {
                var resp = JsonUtility.FromJson<ScoreResponseMessage>(raw);
                Debug.Log($"[MQTT] Score response: session={resp.sessionId}, finalScore={resp.finalScore}");
                OnScoreResponseReceived?.Invoke(resp.sessionId, resp.finalScore);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MQTT] Failed to parse score response: {e.Message}");
            }
        }
    }

    public void Publish(string topic, string json)
    {
        // [IMPROVEMENT 3] Drop the oldest event when the queue is full so newer
        // events are not silently lost and memory stays bounded.
        if (_queue.Count >= MaxQueueSize)
        {
            _queue.TryDequeue(out _);
            Debug.LogWarning("[MQTT] Publish queue full. Dropping oldest event.");
        }
        Debug.Log($"[MQTT] → {topic}: {json}");
        _queue.Enqueue((topic, json));
    }

    public void PublishEvent(string json)   => Publish(topicEvents,  json);
    public void PublishSession(string json) => Publish(topicSession, json);
    public void PublishHse(string json)     => Publish(topicHse,     json);
}
