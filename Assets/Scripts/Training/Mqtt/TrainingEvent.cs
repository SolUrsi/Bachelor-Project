using System;

/// <summary>
/// Structured event model matching the report's header / payload / telemetry design.
///
/// All nested classes are [Serializable] so Unity's JsonUtility can serialize the full
/// nested structure in one JsonUtility.ToJson() call.
///
/// JsonUtility limitation: unused payload fields are included with their zero/empty default
/// values. This is acceptable – the database service stores the full JSONB blob and only
/// reads the fields it needs per event type.
/// </summary>

[Serializable]
public class EventHeader
{
    public string sessionId;
    public string scenarioId;
    public string timestamp;
    public int    sceneId;
    public string eventType;
}

[Serializable]
public class EventPayload
{
    // SESSION_STARTED
    public string scenarioName;

    // SESSION_COMPLETED
    public int   finalScore;
    public float duration;

    // STATE_CHANGED
    public string fromState;
    public string toState;

    // HAZARD_TRIGGERED
    public string hazardId;
    public bool   correct;
    public int    points;

    // HSE_ALERT_EVENT
    public string triggerId;
    public string description;
    public int    penalty;

    // SCORE_UPDATED
    public int    scoreDelta;
    public int    totalScore;
    public string scoreReason;
    public string objectId;

    // USER_ACTION_EVENT
    public string action;
    public string targetObjectId;
    public bool   isSuccess;
}

[Serializable]
public class TelemetryData
{
    public int currentScore;
}

[Serializable]
public class TrainingEvent
{
    public EventHeader   header;
    public EventPayload  payload;
    public TelemetryData telemetry;
}

/// <summary>
/// Published to training/score/request when the scenario ends.
/// The backend calculates the authoritative final score from stored events
/// and publishes the result to training/score/response.
/// </summary>
[Serializable]
public class ScoreRequestMessage
{
    public string sessionId;
    public string type;        // always "SCORE_REQUEST"
}

/// <summary>
/// Received from training/score/response after a SCORE_REQUEST.
/// Contains the backend-calculated authoritative final score.
/// </summary>
[Serializable]
public class ScoreResponseMessage
{
    public string sessionId;
    public int    finalScore;
}
