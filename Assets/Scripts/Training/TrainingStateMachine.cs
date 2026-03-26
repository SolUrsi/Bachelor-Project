using UnityEngine;

/// <summary>
/// Minimal state machine tracking the current phase of a training scenario.
/// 
/// States:
///   - NotStarted: Initial state before session begins
///   - Active: User is interacting with the simulation
///   - Ended: Session has concluded
/// 
/// This is purely for local gameplay flow and UI state.
/// Events are published directly by Hazard, HazardTrigger, and interactable objects.
/// </summary>
public class TrainingStateMachine : MonoBehaviour
{
    public enum TrainingState
    {
        NotStarted,
        Active,
        Ended
    }

    [SerializeField] private TrainingState _currentState = TrainingState.NotStarted;
    public TrainingState CurrentState => _currentState;

    public void SetState(TrainingState newState)
    {
        if (newState == _currentState)
            return;

        var previous = _currentState;
        _currentState = newState;

        #if UNITY_EDITOR
        Debug.Log($"[TrainingState] {previous} ? {_currentState}");
        #endif
    }

    public bool IsActive => _currentState == TrainingState.Active;
}