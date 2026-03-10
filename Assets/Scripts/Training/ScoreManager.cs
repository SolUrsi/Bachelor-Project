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
        int before = totalScore;
        totalScore += -Mathf.Abs(amount);
        totalScore = Mathf.Max(totalScore, 0);
        int actualDelta = totalScore - before; // 0 hvis score allerede var 0

        #if UNITY_EDITOR
        Debug.Log($"[Score] {actualDelta} ({reason}) Total={totalScore}");
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