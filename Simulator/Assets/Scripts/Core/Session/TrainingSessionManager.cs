using System;
using UnityEngine;

/// <summary>
/// Manages the current training session context.
/// 
/// Responsibilities:
///   - Track session ID, scenario ID, and scenario name
///   - Provide context to EventService
///   - Handle session lifecycle
/// 
/// Implements ISessionManager interface for dependency injection.
/// </summary>
public class TrainingSessionManager : MonoBehaviour, ISessionManager
{
    [Header("Scenario Configuration")]
    [SerializeField] private string scenarioId = "SCENARIO-001";
    [SerializeField] private string scenarioName = "HMS Grunnleggende";

    private SessionContext _session;

    public string ScenarioId => scenarioId;
    public string ScenarioName => scenarioName;

    private void Awake()
    {
        _session = new SessionContext();
    }

    private void Start()
    {
        _session.Start(scenarioId, scenarioName);
        Debug.Log($"[SessionManager] Session initialized: {_session.SessionId}");
    }

    public string GetSessionId() => _session?.SessionId ?? "unknown";
    public string GetScenarioId() => scenarioId;
    public string GetScenarioName() => scenarioName;

    public void ResetSession()
    {
        _session?.Reset();
        _session.Start(scenarioId, scenarioName);
        Debug.Log($"[SessionManager] Session reset: {_session.SessionId}");
    }
}
