/// <summary>
/// Interface for game objects that can publish interaction events to the MQTT broker.
/// 
/// Implement this on game objects that the player can interact with (buttons, levers, etc.)
/// to hook into the event publishing system.
/// </summary>
public interface IXRInteractable
{
    /// <summary>
    /// Unique identifier for this interactive object (e.g., "panel_breaker_01").
    /// </summary>
    string ObjectId { get; }

    /// <summary>
    /// Called when the player successfully interacts with this object.
    /// Should publish an ACTION_INTERACT event via EventPublisher.
    /// </summary>
    void OnInteractSuccess(string action, string toolUsed = "none");

    /// <summary>
    /// Called when the player attempts to interact but fails (e.g., missing required tool).
    /// Should publish an ACTION_INTERACT event via EventPublisher with isSuccess = false.
    /// </summary>
    void OnInteractFailure(string action, string toolUsed, string reason, string consequence = "");
}
