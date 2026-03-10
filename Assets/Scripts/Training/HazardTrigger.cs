using UnityEngine;

public class HazardTrigger : MonoBehaviour, IHazardComponent
{
    [Header("Trigger Settings")]
    public string triggerId = "T-001";
    public string description = "Entered unsafe zone";
    public int penaltyPoints = 10;

    [Tooltip("Hvis true: straff kun første gang per session")]
    public bool onlyOnce = true;

    [Tooltip("Kun trigges av objects med denne taggen. Tom = ingen filter.")]
    public string requiredTag = "";

    [Header("Dependencies")]
    [SerializeField] private TrainingScenarioController scenarioController;

    private bool _triggered;

    public string HazardId => triggerId;

    private void Awake()
    {
        // Finn controller hvis ikke satt i Inspector
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

        // Kun aktiv under riktig fase
        if (scenarioController.StateMachine.CurrentState != TrainingStateMachine.TrainingState.IdentifyHazards)
            return;

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            return;

        if (onlyOnce && _triggered) return;

        _triggered = true;

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
        _triggered = false;
    }
}