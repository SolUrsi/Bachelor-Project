using System;
using UnityEngine;

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

    public void LogScenarioStarted(string scenarioId, string scenarioName)
    {
        Log("SCENARIO_STARTED", $"scenarioId={scenarioId}, name={scenarioName}, scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
    }

    public void LogScenarioCompleted(string scenarioId, ScenarioResults results)
    {
        Log("SCENARIO_COMPLETED", $"scenarioId={scenarioId}, score={results.finalScore}, duration={results.duration:F1}s, found={results.hazardsFound}/{results.totalHazards}, missed={results.hazardsMissed}, incorrect={results.incorrectAttempts}, penalties={results.penalties}");
    }

    public void LogScoreBreakdown(ScenarioResults results)
    {
        Log("SCORE_BREAKDOWN", $"correct={results.hazardsFound}, wrong={results.incorrectAttempts}, missed={results.hazardsMissed}, penalties={results.penalties}, score={results.finalScore}, duration={results.duration:F1}s");
    }

    public void LogStateChanged(string from, string to)
    {
        Log("STATE_CHANGED", $"from={from}, to={to}");
    }

    public void LogPointsChanged(int delta, string reason, string objectId, int totalScore)
    {
        Log("POINTS_CHANGED", $"delta={delta}, total={totalScore}, reason={reason}, objectId={objectId}");
    }

    public void LogHazardMarked(string hazardId, bool correct, int delta)
    {
        Log("HAZARD_MARKED", $"hazardId={hazardId}, correct={correct}, delta={delta}");
    }

    public void LogTriggerFired(string triggerId, string description, string colliderName, int delta)
    {
        Log("TRIGGER_FIRED", $"triggerId={triggerId}, desc={description}, by={colliderName}, delta={delta}");
    }

    public void LogSessionEnded(int totalScore)
    {
        Log("SESSION_ENDED", $"totalScore={totalScore}");
    }

    private void Log(string type, string payload)
    {
        var ts = DateTime.UtcNow.ToString("o");
        #if UNITY_EDITOR
        Debug.Log($"[EVENT] {ts} session={sessionId} type={type} payload=({payload})");
        #endif
    }
}