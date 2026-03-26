using System;
using UnityEngine.SceneManagement;

/// <summary>
/// Holds session-scoped identity data that is stamped on every event header.
/// Plain C# class – no MonoBehaviour needed, no scene lifecycle concerns.
/// </summary>
public class SessionContext
{
    public string SessionId    { get; private set; }
    public string ScenarioId   { get; private set; }
    public string ScenarioName { get; private set; }
    public int    SceneId      { get; private set; }

    public void Start(string scenarioId, string scenarioName)
    {
        SessionId    = Guid.NewGuid().ToString("N");
        ScenarioId   = scenarioId;
        ScenarioName = scenarioName;
        SceneId      = SceneManager.GetActiveScene().buildIndex;
    }

    public void Reset()
    {
        SessionId    = null;
        ScenarioId   = null;
        ScenarioName = null;
        SceneId      = 0;
    }
}
