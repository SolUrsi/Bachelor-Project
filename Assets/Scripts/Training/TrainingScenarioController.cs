using System.Collections.Generic;
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
    public float StartTime => _startTime;

    // Created once per scenario run; stamped on every MQTT event header.
    private readonly SessionContext _session = new SessionContext();

    private readonly List<Hazard> _registeredHazards = new List<Hazard>();
    private int _totalCorrectHazards;
    private int _hazardsFound;
    private int _incorrectAttempts;
    private int _totalPenalties;

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
        // Start() runs after all Awake() calls, so EventPublisher.Instance is guaranteed set.
        _session.Start(scenarioId, scenarioName);
        EventPublisher.Instance?.Initialize(_session, scoreManager);
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
        var results = GetScenarioResults();

        stateMachine.SetState(TrainingStateMachine.TrainingState.Completed);
        eventLogger.LogScenarioCompleted(scenarioId, results);  // publishes SESSION_COMPLETED via EventPublisher
        TrainingEvents.RaiseScoreBreakdown(results);
        TrainingEvents.RaiseScenarioCompleted(scenarioId, results.finalScore, results.duration);

        // Ask the backend for the authoritative score calculated from the event history.
        EventPublisher.Instance?.RequestFinalScore();
    }

    /// <summary>
    /// Registrer hazard-komponenter slik at de kan få tilgang til services.
    /// Kalles automatisk fra Hazard.Awake() eller HazardTrigger.Awake().
    /// </summary>
    public void RegisterHazard(IHazardComponent hazard)
    {
        if (hazard is Hazard h)
        {
            _registeredHazards.Add(h);
            if (h.isCorrectHazard) _totalCorrectHazards++;
        }
    }

    public void NotifyHazardFound(string hazardId, int attempts, float timeToFind)
    {
        _hazardsFound++;

        #if UNITY_EDITOR
        Debug.Log($"[Scenario] Hazard found: {hazardId} attempts={attempts} time={timeToFind:F1}s");
        #endif
    }

    public void NotifyIncorrectAttempt()
    {
        _incorrectAttempts++;
    }

    public void NotifyPenalty(int amount)
    {
        _totalPenalties += amount;
    }

    public ScenarioResults GetScenarioResults()
    {
        int missed = _totalCorrectHazards - _hazardsFound;
        return new ScenarioResults
        {
            finalScore        = scoreManager.TotalScore,
            duration          = Time.time - _startTime,
            totalHazards      = _totalCorrectHazards,
            hazardsFound      = _hazardsFound,
            hazardsMissed     = missed,
            incorrectAttempts = _incorrectAttempts,
            penalties         = _totalPenalties
        };
    }
}
