/// <summary>
/// Snapshot av statistikk når et scenario er fullført.
/// Produseres av TrainingScenarioController.GetScenarioResults()
/// og sendes videre via EventLogger og TrainingEvents.
/// </summary>
[System.Serializable]
public class ScenarioResults
{
    public int   finalScore;
    public float duration;
    public int   totalHazards;
    public int   hazardsFound;
    public int   hazardsMissed;
    public int   incorrectAttempts;
    public int   penalties;
}
