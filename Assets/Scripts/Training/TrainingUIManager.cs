using UnityEngine;
using TMPro;

/// <summary>
/// Eksempel på hvordan presentation layer lytter på TrainingEvents.
/// UI, Audio og VFX får all info via event bus - ingen direkte avhengighet til game logic.
/// </summary>
public class TrainingUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private GameObject hazardFeedbackPanel;
    [SerializeField] private TextMeshProUGUI feedbackText;

    private void OnEnable()
    {
        // Subscribe til events
        TrainingEvents.OnScoreChanged += HandleScoreChanged;
        TrainingEvents.OnStateChanged += HandleStateChanged;
        TrainingEvents.OnHazardMarked += HandleHazardMarked;
        TrainingEvents.OnTriggerFired += HandleTriggerFired;
        TrainingEvents.OnScenarioCompleted += HandleScenarioCompleted;
    }

    private void OnDisable()
    {
        // Unsubscribe
        TrainingEvents.OnScoreChanged -= HandleScoreChanged;
        TrainingEvents.OnStateChanged -= HandleStateChanged;
        TrainingEvents.OnHazardMarked -= HandleHazardMarked;
        TrainingEvents.OnTriggerFired -= HandleTriggerFired;
        TrainingEvents.OnScenarioCompleted -= HandleScenarioCompleted;
    }

    private void HandleScoreChanged(int delta, int total)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {total}";
    }

    private void HandleStateChanged(TrainingStateMachine.TrainingState from, TrainingStateMachine.TrainingState to)
    {
        if (stateText != null)
            stateText.text = to.ToString();
    }

    private void HandleHazardMarked(string hazardId, bool correct, int points)
    {
        if (feedbackText != null)
        {
            string message = correct 
                ? $"✓ Correct! +{points}" 
                : $"✗ Wrong hazard -{Mathf.Abs(points)}";
            
            ShowFeedback(message, correct ? Color.green : Color.red);
        }
    }

    private void HandleTriggerFired(string triggerId, string description, int penalty)
    {
        if (feedbackText != null)
        {
            ShowFeedback($"⚠ {description} {penalty}", Color.yellow);
        }
    }

    private void HandleScenarioCompleted(string scenarioId, int finalScore, float duration)
    {
        if (feedbackText != null)
        {
            ShowFeedback($"Scenario Complete!\nScore: {finalScore}\nTime: {duration:F1}s", Color.white);
        }
    }

    private void ShowFeedback(string message, Color color)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = color;
        }

        if (hazardFeedbackPanel != null)
        {
            hazardFeedbackPanel.SetActive(true);
            Invoke(nameof(HideFeedback), 3f);
        }
    }

    private void HideFeedback()
    {
        if (hazardFeedbackPanel != null)
            hazardFeedbackPanel.SetActive(false);
    }
}
