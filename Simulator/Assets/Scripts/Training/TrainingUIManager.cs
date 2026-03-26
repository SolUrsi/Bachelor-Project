using UnityEngine;
using TMPro;

/// <summary>
/// Handles UI updates for the training scenario.
/// 
/// Responsibilities:
///   - Display final score from backend (via ScoreResponseHandler)
///   - Show scenario state changes
///   - Provide visual feedback to the player
/// 
/// This is the ONLY place where score should be displayed to the player.
/// All scoring logic happens on the backend.
/// </summary>
public class TrainingUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TextMeshProUGUI feedbackText;

    private int _displayedScore = 0;

    private void OnEnable()
    {
        ScoreResponseHandler.OnScoreReceived += HandleScoreResponse;
    }

    private void OnDisable()
    {
        ScoreResponseHandler.OnScoreReceived -= HandleScoreResponse;
    }

    private void HandleScoreResponse(string sessionId, int finalScore)
    {
        _displayedScore = finalScore;

        if (scoreText != null)
            scoreText.text = $"Final Score: {finalScore}";

        ShowFeedback($"Scenario Complete!\nFinal Score: {finalScore}", Color.cyan);

        #if UNITY_EDITOR
        Debug.Log($"[UI] Final score received: {finalScore}");
        #endif
    }

    private void ShowFeedback(string message, Color color)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = color;
        }

        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(true);
            Invoke(nameof(HideFeedback), 4f);
        }
    }

    private void HideFeedback()
    {
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);
    }
}
