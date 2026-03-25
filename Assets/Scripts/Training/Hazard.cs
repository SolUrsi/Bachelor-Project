using UnityEngine;

/// <summary>
/// A hazard that the player must identify during the training scenario.
/// 
/// When marked by the player, publishes a HAZARD_MARKED event via EventService.
/// The database system uses these events to calculate the final score.
/// 
/// Prevents duplicate marking via the _marked flag and cooldown system.
/// </summary>
public class Hazard : MonoBehaviour, IHazardComponent
{
    [Header("Hazard Configuration")]
    [SerializeField] private string hazardId = "H-001";
    [SerializeField] private bool isCorrectHazard = true;
    [SerializeField] private int points = 10;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Timing")]
    [SerializeField] private float cooldownSeconds = 1f;

    private TrainingStateMachine _stateMachine;
    private bool _marked = false;
    private float _lastMarkTime = float.MinValue;
    private MaterialPropertyBlock _propertyBlock;

    public string HazardId => hazardId;
    public bool IsMarked => _marked;

    private void Awake()
    {
        _stateMachine = FindFirstObjectByType<TrainingStateMachine>();

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        _propertyBlock = new MaterialPropertyBlock();
    }

    /// <summary>
    /// Called when the player interacts with this hazard.
    /// Publishes a HAZARD_MARKED event to the backend.
    /// </summary>
    public void Mark()
    {
        if (_marked) return;
        if (_stateMachine == null) return;
        if (!_stateMachine.IsActive) return;

        // Anti-spam cooldown
        if (Time.time - _lastMarkTime < cooldownSeconds) return;

        _marked = true;
        _lastMarkTime = Time.time;

        // Publish to backend (positive for correct, negative for wrong)
        int pointValue = isCorrectHazard ? points : -points;
        EventService.Instance?.PublishHazardMarked(hazardId, isCorrectHazard, pointValue);

        // Visual feedback
        SetColor(isCorrectHazard ? Color.green : Color.red);

        #if UNITY_EDITOR
        Debug.Log($"[Hazard] {hazardId} marked ({(isCorrectHazard ? "correct" : "incorrect")})");
        #endif
    }

    private void SetColor(Color c)
    {
        if (targetRenderer == null) return;

        targetRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor("_Color", c);
        targetRenderer.SetPropertyBlock(_propertyBlock);
    }

    public void Reset()
    {
        _marked = false;
        _lastMarkTime = float.MinValue;
        SetColor(Color.white);
    }
}