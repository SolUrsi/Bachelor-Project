using UnityEngine;

/// <summary>
/// Sentral controller for ett treningsscenario. Eier state, score, logging og evaluering.
/// Hazard-komponenter får referanse til denne via Inspector eller Awake.
/// </summary>
public class TrainingScenarioController : MonoBehaviour
{
    [Header("Services")]
    [SerializeField] private TrainingStateMachine stateMachine;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private EventLogger eventLogger;

    [Header("Scenario Config")]
    [SerializeField] private string scenarioId = "SCENARIO-001";
    [SerializeField] private string scenarioName = "HMS Grunnleggende";
    
    public TrainingStateMachine StateMachine => stateMachine;
    public ScoreManager ScoreManager => scoreManager;
    public EventLogger EventLogger => eventLogger;
    
    public string ScenarioId => scenarioId;
    public string ScenarioName => scenarioName;

    private float _startTime;

    private void Awake()
    {
        // Valider at services er tilkoblet
        if (stateMachine == null)
            stateMachine = GetComponentInChildren<TrainingStateMachine>();
        if (scoreManager == null)
            scoreManager = GetComponentInChildren<ScoreManager>();
        if (eventLogger == null)
            eventLogger = GetComponentInChildren<EventLogger>();

        if (stateMachine == null || scoreManager == null || eventLogger == null)
        {
            Debug.LogError($"[ScenarioController] Mangler påkrevde services!", this);
            enabled = false;
            return;
        }

        // Gi services referanse til controller
        stateMachine.Initialize(this);
        scoreManager.Initialize(this);
        eventLogger.Initialize(this);
    }

    private void Start()
    {
        StartScenario();
    }

    public void StartScenario()
    {
        _startTime = Time.time;
        eventLogger.LogScenarioStarted(scenarioId, scenarioName);
        stateMachine.SetState(TrainingStateMachine.TrainingState.Briefing);
    }

    public void EndScenario()
    {
        float duration = Time.time - _startTime;
        int finalScore = scoreManager.TotalScore;

        stateMachine.SetState(TrainingStateMachine.TrainingState.Completed);
        eventLogger.LogScenarioCompleted(scenarioId, finalScore, duration);

        // Publiser til event bus for UI feedback
        TrainingEvents.RaiseScenarioCompleted(scenarioId, finalScore, duration);
    }

    /// <summary>
    /// Registrer hazard-komponenter slik at de kan få tilgang til services.
    /// Kalles automatisk fra Hazard.Awake() eller HazardTrigger.Awake().
    /// </summary>
    public void RegisterHazard(IHazardComponent hazard)
    {
        // Kan brukes til tracking, statistikk, reset, etc.
    }
}
