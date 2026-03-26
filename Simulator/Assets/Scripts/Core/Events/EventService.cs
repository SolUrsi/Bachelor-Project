using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Centralized event publishing service for the training system.
/// 
/// Provides a clean "API" for game systems to publish training events without
/// needing to know about MQTT, JSON serialization, or session context.
/// 
/// Responsibilities:
///   - Coordinate with SessionManager for context (session ID, scenario info)
///   - Build structured TrainingEvent messages
///   - Publish to MQTT via MqttApplicationManager
/// 
/// Thread-safe; can be called from any system.
/// </summary>
public class EventService : MonoBehaviour
{
    public static EventService Instance { get; private set; }

    private ISessionManager _sessionManager;
    private IMqttEventPublisher _mqttPublisher;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Resolve dependencies - search for concrete implementations
        foreach (var manager in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (_sessionManager == null && manager is ISessionManager sm)
                _sessionManager = sm;
            if (_mqttPublisher == null && manager is IMqttEventPublisher mp)
                _mqttPublisher = mp;
        }

        if (_sessionManager == null)
            Debug.LogWarning("[EventService] ISessionManager not found in scene");
        if (_mqttPublisher == null)
            Debug.LogWarning("[EventService] IMqttEventPublisher not found in scene");
    }

    /// <summary>
    /// Publish a session start event.
    /// </summary>
    public void PublishSessionStarted(string scenarioName)
    {
        var evt = BuildEvent("SESSION_START", new EventPayload { scenarioName = scenarioName });
        _mqttPublisher?.PublishEvent(evt);

        #if UNITY_EDITOR
        Debug.Log($"[EventService] Published SESSION_START: {scenarioName}");
        #endif
    }

    /// <summary>
    /// Publish a session end event and request the final score.
    /// </summary>
    public void PublishSessionEnded(float duration)
    {
        var evt = BuildEvent("SESSION_END", new EventPayload { finalScore = 0, duration = duration });
        _mqttPublisher?.PublishEvent(evt);

        #if UNITY_EDITOR
        Debug.Log($"[EventService] Published SESSION_END (duration: {duration:F1}s)");
        #endif
    }

    /// <summary>
    /// Publish a hazard identification event.
    /// </summary>
    public void PublishHazardMarked(string hazardId, bool correct, int points)
    {
        var evt = BuildEvent("HAZARD_MARKED", new EventPayload
        {
            hazardId = hazardId,
            correct = correct,
            points = points
        });
        _mqttPublisher?.PublishEvent(evt);

        #if UNITY_EDITOR
        Debug.Log($"[EventService] Published HAZARD_MARKED: {hazardId} ({(correct ? "correct" : "incorrect")})");
        #endif
    }

    /// <summary>
    /// Publish an HSE safety alert.
    /// </summary>
    public void PublishHseAlert(string triggerId, string description, int penalty)
    {
        var evt = BuildEvent("HSE_ALERT_EVENT", new EventPayload
        {
            triggerId = triggerId,
            description = description,
            penalty = penalty
        });
        _mqttPublisher?.PublishEvent(evt);

        #if UNITY_EDITOR
        Debug.Log($"[EventService] Published HSE_ALERT: {triggerId}");
        #endif
    }

    /// <summary>
    /// Publish a player action/interaction event.
    /// </summary>
    public void PublishActionInteract(string targetObjectId, string action, string toolUsed, bool isSuccess,
                                       string reason = "", string consequence = "")
    {
        var evt = BuildEvent("ACTION_INTERACT", new EventPayload
        {
            targetObjectId = targetObjectId,
            action = action,
            toolUsed = toolUsed,
            isSuccess = isSuccess
        });
        _mqttPublisher?.PublishEvent(evt);

        #if UNITY_EDITOR
        Debug.Log($"[EventService] Published ACTION_INTERACT: {action} on {targetObjectId}");
        #endif
    }

    /// <summary>
    /// Request the authoritative final score from the backend.
    /// Backend will respond on the configured response topic.
    /// </summary>
    public void RequestFinalScore()
    {
        var request = new ScoreRequestMessage
        {
            sessionId = _sessionManager?.GetSessionId() ?? "unknown",
            type = "SCORE_REQUEST"
        };

        _mqttPublisher?.PublishScoreRequest(request);

        #if UNITY_EDITOR
        Debug.Log($"[EventService] Requested final score for session: {request.sessionId}");
        #endif
    }

    // Private: Event building

    private TrainingEvent BuildEvent(string eventType, EventPayload payload)
    {
        var header = new EventHeader
        {
            sessionId = _sessionManager?.GetSessionId() ?? "unknown",
            timestamp = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            sceneId = SceneManager.GetActiveScene().buildIndex,
            eventType = eventType
        };

        var telemetry = new TelemetryData
        {
            currentScore = 0 // Backend calculates score from events
        };

        return new TrainingEvent
        {
            header = header,
            payload = payload,
            telemetry = telemetry
        };
    }
}

/// <summary>
/// Abstraction for session context. Allows EventService to work with any
/// session management system.
/// </summary>
public interface ISessionManager
{
    string GetSessionId();
    string GetScenarioId();
    string GetScenarioName();
}

/// <summary>
/// Abstraction for MQTT event publishing. Allows EventService to remain
/// independent of MQTT transport details.
/// </summary>
public interface IMqttEventPublisher
{
    void PublishEvent(TrainingEvent evt);
    void PublishScoreRequest(ScoreRequestMessage request);
}
