using System;

/// <summary>
/// Static event bus for loose coupling mellom game logic og presentation (UI/Audio/VFX).
/// Brukes IKKE for core game logic (det går via ScenarioController).
/// </summary>
public static class TrainingEvents
{
    // State events
    public static event Action<TrainingStateMachine.TrainingState, TrainingStateMachine.TrainingState> OnStateChanged;

    // Score events
    public static event Action<int, int> OnScoreChanged; // delta, total

    // Hazard events
    public static event Action<string, bool, int> OnHazardMarked; // hazardId, correct, points
    public static event Action<string, string, int> OnTriggerFired; // triggerId, description, penalty

    // Scenario events
    public static event Action<string, int, float> OnScenarioCompleted; // scenarioId, score, duration

    // Helper methods for invoking (optional - kan også kalles direkte)
    public static void RaiseStateChanged(TrainingStateMachine.TrainingState from, TrainingStateMachine.TrainingState to)
    {
        OnStateChanged?.Invoke(from, to);
    }

    public static void RaiseScoreChanged(int delta, int total)
    {
        OnScoreChanged?.Invoke(delta, total);
    }

    public static void RaiseHazardMarked(string hazardId, bool correct, int points)
    {
        OnHazardMarked?.Invoke(hazardId, correct, points);
    }

    public static void RaiseTriggerFired(string triggerId, string description, int penalty)
    {
        OnTriggerFired?.Invoke(triggerId, description, penalty);
    }

    public static void RaiseScenarioCompleted(string scenarioId, int score, float duration)
    {
        OnScenarioCompleted?.Invoke(scenarioId, score, duration);
    }
}
