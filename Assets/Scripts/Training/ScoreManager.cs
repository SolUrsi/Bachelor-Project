using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("Score")]
    [SerializeField] private int totalScore = 0;
    public int TotalScore => totalScore;

    private TrainingScenarioController _controller;
    private EventLogger _logger;

    public void Initialize(TrainingScenarioController controller)
    {
        _controller = controller;
        _logger = controller.EventLogger;
    }

    public void AddPoints(int amount, string reason, string objectId = null)
    {
        totalScore += amount;

        #if UNITY_EDITOR
        Debug.Log($"[Score] +{amount} ({reason}) Total={totalScore}");
        #endif

        if (_logger != null)
            _logger.LogPointsChanged(amount, reason, objectId, totalScore);

        TrainingEvents.RaiseScoreChanged(amount, totalScore);
    }

    public void DeductPoints(int amount, string reason, string objectId = null)
    {
        int delta = -Mathf.Abs(amount);
        totalScore += delta;

        #if UNITY_EDITOR
        Debug.Log($"[Score] {delta} ({reason}) Total={totalScore}");
        #endif

        if (_logger != null)
            _logger.LogPointsChanged(delta, reason, objectId, totalScore);

        TrainingEvents.RaiseScoreChanged(delta, totalScore);
    }

    public void ResetScore()
    {
        totalScore = 0;
    }
}