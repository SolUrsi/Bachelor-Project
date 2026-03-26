using UnityEngine;

/// <summary>
/// Placeholder for backward compatibility with existing code that may reference ScoreManager.
/// 
/// In the new architecture, score is NOT tracked locally. The database system calculates
/// the authoritative final score from published events (HAZARD_MARKED, HSE_ALERT_EVENT, etc.).
/// 
/// The Unity client only receives the final score after requesting it via EventPublisher.RequestFinalScore(),
/// which triggers the database to respond on the "response/points" topic.
/// 
/// This class can be removed once all references are updated to use EventPublisher directly.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // Intentionally empty; kept for compatibility
}