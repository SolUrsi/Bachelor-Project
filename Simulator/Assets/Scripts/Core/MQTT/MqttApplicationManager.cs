using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

/// <summary>
/// Application-wide MQTT connection manager.
/// 
/// Initializes automatically on application startup (before any scene loads).
/// Remains connected for the lifetime of the application.
/// 
/// Responsibilities:
///   - Establish and maintain MQTT broker connection
///   - Handle automatic reconnection on network failure
///   - Queue and publish events asynchronously
///   - Receive and dispatch incoming messages
/// 
/// Thread-safe and non-blocking; all operations are async.
/// </summary>
[DefaultExecutionOrder(-100)] // Run before all other scripts
public class MqttApplicationManager : MonoBehaviour
{
    public static MqttApplicationManager Instance { get; private set; }

    [Header("Broker Configuration")]
    [SerializeField] private string brokerHost = "157.245.79.217";
    [SerializeField] private int brokerPort = 1883;
    [SerializeField] private int connectionTimeoutMs = 5000;
    [SerializeField] private int reconnectDelayMs = 5000;

    [Header("Topic Configuration")]
    [SerializeField] private string topicEventPublish = "events/";
    [SerializeField] private string topicScoreRequest = "request/points";
    [SerializeField] private string topicScoreResponse = "response/points";

    /// <summary>
    /// Fired on the main thread when a message is received.
    /// </summary>
    public static event Action<string, string> OnMessageReceived;

    /// <summary>
    /// Fired on the main thread when connection status changes.
    /// </summary>
    public static event Action<bool> OnConnectionStatusChanged;

    private IMqttClient _client;
    private bool _isConnected = false;
    private bool _isConnecting = false;
    private CancellationTokenSource _connectionCts;
    private string _clientId;
    private ConcurrentQueue<(string topic, string payload)> _messageQueue;
    private ConcurrentQueue<(string topic, string payload)> _publishQueue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _messageQueue = new ConcurrentQueue<(string, string)>();
        _publishQueue = new ConcurrentQueue<(string, string)>();
        _clientId = $"traftec-vr-{SystemInfo.deviceUniqueIdentifier.GetHashCode()}";

        Debug.Log($"[MQTT] Application manager initialized (Client ID: {_clientId})");
    }

    private void Start()
    {
        ConnectAsync();
    }

    private void OnDestroy()
    {
        DisconnectAsync().Wait(2000);
    }

    private void Update()
    {
        // Process queued incoming messages on main thread
        while (_messageQueue.TryDequeue(out var message))
        {
            OnMessageReceived?.Invoke(message.topic, message.payload);
        }
    }

    /// <summary>
    /// Publish a message to a specific MQTT topic.
    /// Non-blocking; message is queued and published asynchronously.
    /// </summary>
    public void Publish(string topic, string payload)
    {
        if (string.IsNullOrEmpty(topic))
        {
            Debug.LogWarning("[MQTT] Cannot publish: topic is null or empty");
            return;
        }

        _publishQueue.Enqueue((topic, payload));

        #if UNITY_EDITOR
        Debug.Log($"[MQTT] Queued publish to '{topic}'");
        #endif
    }

    /// <summary>
    /// Subscribe to a specific MQTT topic.
    /// Must be called while connected.
    /// </summary>
    public async Task SubscribeAsync(string topic)
    {
        if (_client?.IsConnected != true)
        {
            Debug.LogWarning($"[MQTT] Cannot subscribe to '{topic}': not connected");
            return;
        }

        try
        {
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(new MqttTopicFilterBuilder().WithTopic(topic).Build())
                .Build();

            await _client.SubscribeAsync(subscribeOptions, _connectionCts.Token);
            Debug.Log($"[MQTT] Subscribed to '{topic}'");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MQTT] Failed to subscribe to '{topic}': {ex.Message}");
        }
    }

    public bool IsConnected => _isConnected;
    public string BrokerHost => brokerHost;
    public int BrokerPort => brokerPort;

    // Private: Connection and MQTT operations

    private void ConnectAsync()
    {
        if (_isConnecting || _isConnected) return;
        Task.Run(async () => await ConnectAsyncInternal());
    }

    private async Task ConnectAsyncInternal()
    {
        _isConnecting = true;
        _connectionCts = new CancellationTokenSource();

        while (!_isConnected)
        {
            try
            {
                var factory = new MqttFactory();
                _client = factory.CreateMqttClient();

                _client.DisconnectedAsync += OnClientDisconnected;
                _client.ApplicationMessageReceivedAsync += OnMessageReceivedInternal;

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(brokerHost, brokerPort)
                    .WithClientId(_clientId)
                    .WithCleanSession(true)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                    .Build();

                var connectResult = await _client.ConnectAsync(options, _connectionCts.Token);

                if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
                {
                    _isConnected = true;
                    _isConnecting = false;

                    Debug.Log($"[MQTT] Connected to {brokerHost}:{brokerPort}");
                    OnConnectionStatusChanged?.Invoke(true);

                    // Subscribe to response topics
                    await SubscribeAsync(topicScoreResponse);

                    // Start publishing queued events
                    PublishQueuedEventsAsync();
                    return;
                }
                else
                {
                    Debug.LogWarning($"[MQTT] Connection failed: {connectResult.ResultCode}. Retrying in {reconnectDelayMs}ms...");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MQTT] Connection error: {ex.Message}. Retrying in {reconnectDelayMs}ms...");
            }

            try
            {
                await Task.Delay(reconnectDelayMs, _connectionCts.Token);
            }
            catch (OperationCanceledException) { }
        }

        _isConnecting = false;
    }

    private Task OnClientDisconnected(MqttClientDisconnectedEventArgs e)
    {
        if (_isConnected)
        {
            Debug.LogWarning("[MQTT] Connection lost. Attempting to reconnect...");
            _isConnected = false;
            OnConnectionStatusChanged?.Invoke(false);
            ConnectAsync();
        }
        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedInternal(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            string payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            _messageQueue.Enqueue((e.ApplicationMessage.Topic, payload));
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MQTT] Failed to process message: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task PublishQueuedEventsAsync()
    {
        while (_isConnected)
        {
            try
            {
                bool published = false;

                while (_isConnected && _publishQueue.TryDequeue(out var item))
                {
                    try
                    {
                        var applicationMessage = new MqttApplicationMessageBuilder()
                            .WithTopic(item.topic)
                            .WithPayload(Encoding.UTF8.GetBytes(item.payload))
                            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                            .Build();

                        await _client.PublishAsync(applicationMessage, _connectionCts.Token);
                        published = true;

                        #if UNITY_EDITOR
                        Debug.Log($"[MQTT] Published to '{item.topic}'");
                        #endif
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[MQTT] Failed to publish: {ex.Message}");
                        break;
                    }
                }

                if (!published)
                    await Task.Delay(100, _connectionCts.Token);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MQTT] Publishing loop error: {ex.Message}");
                await Task.Delay(1000, _connectionCts.Token);
            }
        }
    }

    private async Task DisconnectAsync()
    {
        if (_client?.IsConnected == true)
        {
            try
            {
                _connectionCts?.Cancel();
                await _client.DisconnectAsync();
                Debug.Log("[MQTT] Disconnected");
            }
            catch { }
        }
    }
}
