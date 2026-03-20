using UnityEngine;

/// <summary>
/// A trigger volume that fires an HSE_ALERT_EVENT when the player enters while active.
/// 
/// Publishes events via EventService; backend receives and processes these.
/// 
/// Supports:
///   - One-time triggering (onlyOnce flag)
///   - Cooldown to prevent spam from physics glitches
///   - Tag filtering to only trigger on specific objects
/// </summary>
public class HazardTrigger : MonoBehaviour, IHazardComponent
{
    [Header("Trigger Configuration")]
    [SerializeField] private string triggerId = "T-001";
    [SerializeField] private string description = "Entered unsafe zone";
    [SerializeField] private int penaltyPoints = 10;

    [Header("Behavior")]
    [SerializeField] private bool onlyOnce = true;
    [SerializeField] private float cooldownSeconds = 1f;
    [SerializeField] private string requiredTag = "";

    private TrainingStateMachine _stateMachine;
    private bool _triggered = false;
    private float _lastTriggerTime = float.MinValue;

    public string HazardId => triggerId;

    private void Awake()
    {
        _stateMachine = FindFirstObjectByType<TrainingStateMachine>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_stateMachine == null || !_stateMachine.IsActive)
            return;

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        if (onlyOnce && _triggered)
            return;

        if (Time.time - _lastTriggerTime < cooldownSeconds)
            return;

        _triggered = true;
        _lastTriggerTime = Time.time;

        // Publish HSE alert to backend
        EventService.Instance?.PublishHseAlert(triggerId, description, penaltyPoints);

        #if UNITY_EDITOR
        Debug.Log($"[HazardTrigger] {triggerId} fired by {other.name}");
        #endif
    }

    public void Reset()
    {
        _triggered = false;
        _lastTriggerTime = float.MinValue;
    }
}