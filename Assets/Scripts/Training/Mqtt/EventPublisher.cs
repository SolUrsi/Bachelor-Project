using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central event creation and routing service.
///
/// Gameplay scripts (Hazard, HazardTrigger, ScoreManager, TrainingStateMachine) never
/// touch JSON or MQTT topics directly. They either call EventPublisher.Publish*() directly
/// or go through the EventLogger compatibility wrapper.
///
/// MqttService remains transport-only. EventPublisher owns:
///   - building the header / payload / telemetry event structure
///   - routing each event type to the correct MQTT topic
///
/// Initialized by TrainingScenarioController before any gameplay events are raised.
/// </summary>
public class EventPublisher : MonoBehaviour
{
    public static EventPublisher Instance { get; private set; }

    private SessionContext _session;
    private ScoreManager   _scoreManager;

    private const string TopicEvents  = "training/events";
    private const string TopicSession = "training/session";
    private const string TopicHse     = "training/hse";

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Initialize(SessionContext session, ScoreManager scoreManager)
    {
        _session      = session;
        _scoreManager = scoreManager;
    }

    // ── Public API ──────────────────────────────────────────────────────────────

    public void PublishSessionStarted(string scenarioName)
    {
        Send(TopicSession, Build("SESSION_STARTED", new EventPayload
        {
            scenarioName = scenarioName
        }));
    }

    public void PublishSessionCompleted(int finalScore, float duration)
    {
        Send(TopicSession, Build("SESSION_COMPLETED", new EventPayload
        {
            finalScore = finalScore,
            duration   = duration
        }));
    }

    public void PublishStateChanged(string fromState, string toState)
    {
        Send(TopicEvents, Build("STATE_CHANGED", new EventPayload
        {
            fromState = fromState,
            toState   = toState
        }));
    }

    public void PublishHazardTriggered(string hazardId, bool correct, int points)
    {
        Send(TopicEvents, Build("HAZARD_TRIGGERED", new EventPayload
        {
            hazardId = hazardId,
            correct  = correct,
            points   = points
        }));
    }

    public void PublishHseAlert(string triggerId, string description, int penalty)
    {
        Send(TopicHse, Build("HSE_ALERT_EVENT", new EventPayload
        {
            triggerId   = triggerId,
            description = description,
            penalty     = penalty
        }));
    }

    public void PublishScoreUpdated(int scoreDelta, int totalScore, string reason, string objectId)
    {
        Send(TopicEvents, Build("SCORE_UPDATED", new EventPayload
        {
            scoreDelta  = scoreDelta,
            totalScore  = totalScore,
            scoreReason = reason,
            objectId    = objectId ?? ""
        }));
    }

    public void PublishUserAction(string action, string targetObjectId, bool isSuccess)
    {
        Send(TopicEvents, Build("USER_ACTION_EVENT", new EventPayload
        {
            action         = action,
            targetObjectId = targetObjectId ?? "",
            isSuccess      = isSuccess,
            totalScore     = _scoreManager?.TotalScore ?? 0
        }));
    }

    // ── Private helpers ─────────────────────────────────────────────────────────

    private TrainingEvent Build(string eventType, EventPayload payload)
    {
        return new TrainingEvent
        {
            header = new EventHeader
            {
                sessionId  = _session?.SessionId  ?? "",
                scenarioId = _session?.ScenarioId ?? "",
                timestamp  = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                sceneId    = SceneManager.GetActiveScene().buildIndex,
                eventType  = eventType
            },
            payload   = payload,
            telemetry = new TelemetryData
            {
                currentScore = _scoreManager?.TotalScore ?? 0
            }
        };
    }

    private void Send(string topic, TrainingEvent evt)
    {
        if (MqttService.Instance == null) return;
        MqttService.Instance.Publish(topic, JsonUtility.ToJson(evt));
    }
}
