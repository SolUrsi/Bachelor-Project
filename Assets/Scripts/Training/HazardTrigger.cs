using UnityEngine;

public class HazardTrigger : MonoBehaviour, IHazardComponent
{
    [Header("Trigger Settings")]
    public string triggerId    = "T-001";
    public string description  = "Entered unsafe zone";
    public int    penaltyPoints = 10;

    [Tooltip("Hvis true: straff kun første gang per session.")]
    public bool onlyOnce = true;

    [Tooltip("Minimum sekunder mellom to påfølgende events fra denne triggeren (gjelder kun når onlyOnce = false). " +
             "Forhindrer event-spam ved fysikk-glitcher eller raske gjentatte inntrengninger.")]
    [SerializeField] private float cooldownSeconds = 1f;

    [Tooltip("Kun trigges av objects med denne taggen. Tom = ingen filter.")]
    public string requiredTag = "";

    [Header("Dependencies")]
    [SerializeField] private TrainingScenarioController scenarioController;

    private bool  _triggered;
    private float _lastTriggerTime = float.MinValue;

    public string HazardId => triggerId;

    private void Awake()
    {
        if (scenarioController == null)
            scenarioController = FindFirstObjectByType<TrainingScenarioController>();

        if (scenarioController == null)
        {
            Debug.LogError($"[HazardTrigger] {triggerId}: Ingen TrainingScenarioController funnet!", this);
            enabled = false;
            return;
        }

        scenarioController.RegisterHazard(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (scenarioController == null) return;

        // Kun aktiv under riktig fase.
        if (scenarioController.StateMachine.CurrentState != TrainingStateMachine.TrainingState.IdentifyHazards)
            return;

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        // Primary guard: fire at most once per session when onlyOnce = true.
        if (onlyOnce && _triggered) return;

        // Anti-spam guard: enforce minimum cooldown between events.
        // Prevents duplicate events from physics glitches or rapid boundary crossings
        // in XR where the controller can clip a collider multiple times in < 1 frame.
        if (Time.time - _lastTriggerTime < cooldownSeconds) return;

        _triggered      = true;
        _lastTriggerTime = Time.time;

        scenarioController.ScoreManager.DeductPoints(penaltyPoints, $"Trigger: {description}", triggerId);
        scenarioController.EventLogger.LogTriggerFired(triggerId, description, other.name, -penaltyPoints);
        scenarioController.NotifyPenalty(penaltyPoints);
        TrainingEvents.RaiseTriggerFired(triggerId, description, -penaltyPoints);

        #if UNITY_EDITOR
        Debug.Log($"[HazardTrigger] {triggerId} fired by {other.name}");
        #endif
    }

    public void Reset()
    {
        _triggered       = false;
        _lastTriggerTime = float.MinValue;
    }
}