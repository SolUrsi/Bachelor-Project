using System;
using System.Collections.Generic;

/// <summary>
/// Structured event model matching the header / payload / telemetry design.
/// All nested classes are [Serializable] so Unity's JsonUtility can serialize them.
/// </summary>

[Serializable]
public class EventHeader
{
    public string sessionId;
    public string timestamp;
    public int    sceneId;
    public string eventType;
}

[Serializable]
public class EventMetadata
{
    public string reason;
    public string consequenceTriggered;
}

[Serializable]
public class EventPayload
{
    // SESSION_START
    public string scenarioName;

    // SESSION_END
    public int   finalScore;
    public float duration;

    // ACTION_INTERACT
    public string targetObjectId;
    public string action;
    public string toolUsed;
    public bool   isSuccess;
    
    [NonSerialized]
    public EventMetadata metadata;

    // General purpose fields
    public string hazardId;
    public bool   correct;
    public int    points;
    public string triggerId;
    public string description;
    public int    penalty;
}

[Serializable]
public class TelemetryData
{
    public int currentScore;
    public Vector3Data playerPosition;
}

[Serializable]
public class Vector3Data
{
    public float x;
    public float y;
    public float z;

    public Vector3Data() { }
    
    public Vector3Data(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

[Serializable]
public class TrainingEvent
{
    public EventHeader   header;
    public EventPayload  payload;
    public TelemetryData telemetry;
}

/// <summary>
/// Published to request/points when requesting final score.
/// </summary>
[Serializable]
public class ScoreRequestMessage
{
    public string sessionId;
    public string type = "SCORE_REQUEST";
}

/// <summary>
/// Received from response/points after a SCORE_REQUEST.
/// </summary>
[Serializable]
public class ScoreResponseMessage
{
    public string sessionId;
    public int    finalScore;
}
