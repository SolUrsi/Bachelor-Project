using UnityEngine;

public class TrainingStateMachine : MonoBehaviour
{
    public enum TrainingState
    {
        NotStarted,
        Briefing,
        IdentifyHazards,
        Review,
        Completed
    }

    [Header("State")]
    [SerializeField] private TrainingState currentState = TrainingState.NotStarted;
    public TrainingState CurrentState => currentState;

    private TrainingScenarioController _controller;
    private ScoreManager _score;
    private EventLogger _logger;

    public void Initialize(TrainingScenarioController controller)
    {
        _controller = controller;
        _score = controller.ScoreManager;
        _logger = controller.EventLogger;
    }

    public void SetState(TrainingState newState)
    {
        if (newState == currentState) return;

        var from = currentState;
        currentState = newState;

        #if UNITY_EDITOR
        Debug.Log($"[State] {from} -> {currentState}");
        #endif

        if (_logger != null) 
            _logger.LogStateChanged(from.ToString(), currentState.ToString());

        TrainingEvents.RaiseStateChanged(from, currentState);
    }

    // Kalles fra UI-knapp eller start-logikk
    public void StartHazardPhase()
    {
        if (currentState != TrainingState.Briefing) return;
        SetState(TrainingState.IdentifyHazards);
    }

    public void FinishHazardPhase()
    {
        if (currentState != TrainingState.IdentifyHazards) return;
        SetState(TrainingState.Review);
    }

    public void CompleteSession()
    {
        if (currentState != TrainingState.Review) return;
        SetState(TrainingState.Completed);

        if (_logger != null && _score != null)
            _logger.LogSessionEnded(_score.TotalScore);
    }
}