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

    private const string TopicEvents       = "training/events";
    private const string TopicSession      = "training/session";
    private const string TopicHse          = "training/hse";
    private const string TopicScoreRequest = "training/score/request";

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

    /// <summary>
    /// Publishes SESSION_COMPLETED to training/session.
    /// <para>
    /// <paramref name="finalScore"/> is Unity's LOCAL running score at the moment the
    /// scenario ends. It is included in the payload as a convenience snapshot for the
    /// database, but it is NOT the authoritative final score.
    /// The authoritative score is calculated by the backend from the full event history
    /// and returned via training/score/response after <see cref="RequestFinalScore"/>.
    /// </para>
    /// </summary>
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

    /// <summary>
    /// Publishes a HAZARD_MARKED event. <paramref name="points"/> is the signed
    /// delta: positive for a correct identification, negative for a wrong one.
    /// </summary>
    public void PublishHazardMarked(string hazardId, bool correct, int points)
    {
        Send(TopicEvents, Build("HAZARD_MARKED", new EventPayload
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

    /// <summary>
    /// Publishes a SCORE_UPDATED telemetry event to training/events.
    /// <para>
    /// This event is supplementary telemetry – the backend does NOT use <c>totalScore</c>
    /// here to calculate the authoritative final score. Score calculation is derived
    /// solely from HAZARD_MARKED and HSE_ALERT_EVENT events.
    /// <c>totalScore</c> is Unity's non-authoritative local running total, included
    /// only so developers can cross-check the event stream during debugging.
    /// </para>
    /// </summary>
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

    /// <summary>
    /// Publishes a SCORE_REQUEST to training/score/request.
    /// The backend calculates the authoritative final score from the stored event
    /// history and responds on training/score/response.
    /// </summary>
    public void RequestFinalScore()
    {
        if (MqttService.Instance == null) return;

        var request = new ScoreRequestMessage
        {
            sessionId = _session?.SessionId ?? "",
            type      = "SCORE_REQUEST"
        };

        MqttService.Instance.Publish(TopicScoreRequest, JsonUtility.ToJson(request));
        Debug.Log($"[EventPublisher] Score request sent for session={_session?.SessionId}");
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
