using UnityEngine;

/// <summary>
/// Tracks a LOCAL, non-authoritative running score for IMMEDIATE UI feedback only.
///
/// This score is intentionally separate from the backend's authoritative final score.
/// The database system calculates the real final score from the published event history
/// (HAZARD_MARKED and HSE_ALERT_EVENT events).
///
/// Unity uses this local value solely so the player sees instant point-change feedback
/// (green +10, red -5) without waiting for a network round-trip.
/// The authoritative score is received via MqttService.OnScoreResponseReceived
/// after TrainingScenarioController.EndScenario() sends a SCORE_REQUEST.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    [Header("Local Score (non-authoritative – for UI feedback only)")]
    [SerializeField] private int totalScore = 0;

    /// <summary>Unity's local running score. Non-authoritative – for immediate feedback only.</summary>
    public int TotalScore => totalScore;

    private TrainingScenarioController _controller;
    private EventLogger _logger;

    public void Initialize(TrainingScenarioController controller)
    {
        _controller = controller;
        _logger     = controller.EventLogger;
    }

    public void AddPoints(int amount, string reason, string objectId = null)
    {
        totalScore += amount;

        #if UNITY_EDITOR
        Debug.Log($"[Score] +{amount} ({reason}) LocalTotal={totalScore}");
        #endif

        if (_logger != null)
            _logger.LogPointsChanged(amount, reason, objectId, totalScore);

        TrainingEvents.RaiseScoreChanged(amount, totalScore);
    }

    public void DeductPoints(int amount, string reason, string objectId = null)
    {
        int before      = totalScore;
        totalScore     += -Mathf.Abs(amount);
        totalScore      = Mathf.Max(totalScore, 0);
        int actualDelta = totalScore - before; // 0 if score was already 0

        #if UNITY_EDITOR
        Debug.Log($"[Score] {actualDelta} ({reason}) LocalTotal={totalScore}");
        #endif

        if (_logger != null)
            _logger.LogPointsChanged(actualDelta, reason, objectId, totalScore);

        TrainingEvents.RaiseScoreChanged(actualDelta, totalScore);
    }

    public void ResetScore()
    {
        totalScore = 0;
    }
}