using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central coordinator for a training scenario lifecycle.
/// 
/// Responsibilities:
///   - Manage scenario state transitions (start, end, reset)
///   - Coordinate with EventService for publishing scenario events
///   - Track scenario duration and metadata
///   - Manage stateMachine state
/// 
/// Works closely with TrainingStateMachine for gameplay flow control.
/// </summary>
public class TrainingScenarioController : MonoBehaviour
{
    [SerializeField] private TrainingStateMachine stateMachine;

    private float _scenarioStartTime;

    public TrainingStateMachine StateMachine => stateMachine;

    private void Awake()
    {
        if (stateMachine == null)
            stateMachine = FindFirstObjectByType<TrainingStateMachine>();

        if (stateMachine == null)
        {
            Debug.LogError("[TrainingScenarioController] TrainingStateMachine not found!", this);
            enabled = false;
        }
    }

    /// <summary>
    /// Called when the scenario officially starts (e.g., UI button pressed).
    /// Publishes SESSION_START event via EventService.
    /// </summary>
    public void StartScenario()
    {
        if (stateMachine.CurrentState != TrainingStateMachine.TrainingState.NotStarted)
        {
            Debug.LogWarning("[TrainingScenarioController] Scenario already started");
            return;
        }

        _scenarioStartTime = Time.time;
        stateMachine.SetState(TrainingStateMachine.TrainingState.Active);

        var sessionManager = FindFirstObjectByType<TrainingSessionManager>();
        string scenarioName = sessionManager?.ScenarioName ?? "Training Scenario";

        EventService.Instance?.PublishSessionStarted(scenarioName);

        Debug.Log($"[TrainingScenarioController] Scenario started");
    }

    /// <summary>
    /// Called when the scenario officially ends (e.g., all hazards found, time up).
    /// Publishes SESSION_END event and requests the authoritative final score.
    /// </summary>
    public void EndScenario()
    {
        if (stateMachine.CurrentState != TrainingStateMachine.TrainingState.Active)
        {
            Debug.LogWarning("[TrainingScenarioController] Scenario not active");
            return;
        }

        float duration = Time.time - _scenarioStartTime;
        stateMachine.SetState(TrainingStateMachine.TrainingState.Ended);

        EventService.Instance?.PublishSessionEnded(duration);
        EventService.Instance?.RequestFinalScore();

        Debug.Log($"[TrainingScenarioController] Scenario ended (duration: {duration:F1}s)");
    }

    /// <summary>
    /// Reset session state for a new run (e.g., "Try Again").
    /// </summary>
    public void ResetScenario()
    {
        stateMachine.SetState(TrainingStateMachine.TrainingState.NotStarted);

        var sessionManager = FindFirstObjectByType<TrainingSessionManager>();
        sessionManager?.ResetSession();

        // Reset all hazards and triggers
        foreach (var hazard in FindObjectsByType<Hazard>(FindObjectsSortMode.None))
            hazard.Reset();

        foreach (var trigger in FindObjectsByType<HazardTrigger>(FindObjectsSortMode.None))
            trigger.Reset();

        Debug.Log("[TrainingScenarioController] Scenario reset");
    }
}
