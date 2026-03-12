using UnityEngine;

public class Hazard : MonoBehaviour, IHazardComponent
{
    [Header("Hazard")]
    public string hazardId = "H-001";
    public bool isCorrectHazard = true;

    [Header("Scoring")]
    public int pointsIfCorrect = 10;
    public int penaltyIfWrong = 5;

    [Header("Visual feedback (optional)")]
    public Renderer targetRenderer;

    [Header("Dependencies")]
    [SerializeField] private TrainingScenarioController scenarioController;

    private bool _marked;
    private int  _attempts;
    private MaterialPropertyBlock _propertyBlock;

    public string HazardId => hazardId;
    public bool IsMarked => _marked;

    private void Awake()
    {
        // Finn controller hvis ikke satt i Inspector
        if (scenarioController == null)
            scenarioController = FindFirstObjectByType<TrainingScenarioController>();

        if (scenarioController == null)
        {
            Debug.LogError($"[Hazard] {hazardId}: Ingen TrainingScenarioController funnet!", this);
            enabled = false;
            return;
        }

        scenarioController.RegisterHazard(this);

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        _propertyBlock = new MaterialPropertyBlock();
    }

    // Denne kan du kalle fra XR "Select Entered" i Inspector
    public void Mark()
    {
        if (_marked) return;
        if (scenarioController == null) return;

        if (scenarioController.StateMachine.CurrentState != TrainingStateMachine.TrainingState.IdentifyHazards)
            return;

        _attempts++;
        _marked = true;

        if (isCorrectHazard)
        {
            float timeToFind = Time.time - scenarioController.StartTime;
            scenarioController.ScoreManager.AddPoints(pointsIfCorrect, "Correct hazard identified", hazardId);
            scenarioController.EventLogger.LogHazardMarked(hazardId, true, pointsIfCorrect);
            scenarioController.NotifyHazardFound(hazardId, _attempts, timeToFind);
            TrainingEvents.RaiseHazardMarked(hazardId, true, pointsIfCorrect);
            SetColor(Color.green);
        }
        else
        {
            scenarioController.ScoreManager.DeductPoints(penaltyIfWrong, "Incorrect hazard marked", hazardId);
            scenarioController.EventLogger.LogHazardMarked(hazardId, false, -penaltyIfWrong);
            scenarioController.NotifyIncorrectAttempt();
            TrainingEvents.RaiseHazardMarked(hazardId, false, -penaltyIfWrong);
            SetColor(Color.red);
        }
    }

    private void SetColor(Color c)
    {
        if (targetRenderer == null) return;

        // Bruk MaterialPropertyBlock for å unngå material-instansiering
        targetRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor("_Color", c);
        targetRenderer.SetPropertyBlock(_propertyBlock);
    }

    public void Reset()
    {
        _marked = false;
        SetColor(Color.white);
    }
}