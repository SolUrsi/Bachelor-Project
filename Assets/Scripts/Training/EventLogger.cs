using System;
using UnityEngine;

/// <summary>
/// Two responsibilities:
///   1. Local debug logging (editor-only) – unchanged.
///   2. Forward every gameplay event to EventPublisher so it is built into
///      structured JSON and published to the MQTT broker immediately.
///
/// EventPublisher.Instance is resolved lazily so that Awake() ordering between
/// GameObjects does not matter; EventPublisher is always initialised by
/// TrainingScenarioController.Start() before the first gameplay event fires.
/// </summary>
public class EventLogger : MonoBehaviour
{
    [Header("Session")]
    [SerializeField] private string sessionId;

    private TrainingScenarioController _controller;

    public void Initialize(TrainingScenarioController controller)
    {
        _controller = controller;

        if (string.IsNullOrEmpty(sessionId))
            sessionId = Guid.NewGuid().ToString("N");
    }

    // ── Scenario lifecycle ────────────────────────────────────────────────────

    public void LogScenarioStarted(string scenarioId, string scenarioName)
    {
        Log("SESSION_STARTED", $"scenarioId={scenarioId}, name={scenarioName}, scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        EventPublisher.Instance?.PublishSessionStarted(scenarioName);
    }

    public void LogScenarioCompleted(string scenarioId, ScenarioResults results)
    {
        Log("SESSION_COMPLETED", $"scenarioId={scenarioId}, score={results.finalScore}, duration={results.duration:F1}s, found={results.hazardsFound}/{results.totalHazards}, missed={results.hazardsMissed}, incorrect={results.incorrectAttempts}, penalties={results.penalties}");
        EventPublisher.Instance?.PublishSessionCompleted(results.finalScore, results.duration);
    }

    /// <summary>Local debug only – score breakdown is reconstructed by the backend from event history.</summary>
    public void LogScoreBreakdown(ScenarioResults results)
    {
        Log("SCORE_BREAKDOWN", $"correct={results.hazardsFound}, wrong={results.incorrectAttempts}, missed={results.hazardsMissed}, penalties={results.penalties}, score={results.finalScore}, duration={results.duration:F1}s");
    }

    // ── State machine ─────────────────────────────────────────────────────────

    public void LogStateChanged(string from, string to)
    {
        Log("STATE_CHANGED", $"from={from}, to={to}");
        EventPublisher.Instance?.PublishStateChanged(from, to);
    }

    // ── Score ─────────────────────────────────────────────────────────────────

    public void LogPointsChanged(int delta, string reason, string objectId, int totalScore)
    {
        Log("POINTS_CHANGED", $"delta={delta}, total={totalScore}, reason={reason}, objectId={objectId}");
        EventPublisher.Instance?.PublishScoreUpdated(delta, totalScore, reason, objectId);
    }

    // ── Hazards ───────────────────────────────────────────────────────────────

    /// <summary>
    /// <paramref name="delta"/> is the signed point change:
    /// positive for a correct identification, negative for a wrong one.
    /// </summary>
    public void LogHazardMarked(string hazardId, bool correct, int delta)
    {
        Log("HAZARD_MARKED", $"hazardId={hazardId}, correct={correct}, delta={delta}");
        EventPublisher.Instance?.PublishHazardMarked(hazardId, correct, delta);
    }

    /// <summary>
    /// Maps to HSE_ALERT_EVENT on the training/hse topic.
    /// <paramref name="delta"/> is negative (penalty already applied by ScoreManager).
    /// </summary>
    public void LogTriggerFired(string triggerId, string description, string colliderName, int delta)
    {
        Log("HSE_ALERT_EVENT", $"triggerId={triggerId}, desc={description}, by={colliderName}, delta={delta}");
        EventPublisher.Instance?.PublishHseAlert(triggerId, description, Math.Abs(delta));
    }

    /// <summary>Local debug only – SESSION_COMPLETED is the canonical session-end event.</summary>
    public void LogSessionEnded(int totalScore)
    {
        Log("SESSION_ENDED", $"totalScore={totalScore}");
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void Log(string type, string payload)
    {
        var ts = DateTime.UtcNow.ToString("o");
        #if UNITY_EDITOR
        Debug.Log($"[EVENT] {ts} session={sessionId} type={type} payload=({payload})");
        #endif
    }
}