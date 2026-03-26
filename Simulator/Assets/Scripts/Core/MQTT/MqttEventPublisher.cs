using System.Text;
using UnityEngine;

/// <summary>
/// Implements IMqttEventPublisher to handle serialization and publishing
/// of training events via MQTT.
/// 
/// Responsibilities:
///   - Serialize training events to JSON
///   - Publish via MqttApplicationManager
///   - Handle message formatting
/// </summary>
public class MqttEventPublisher : MonoBehaviour, IMqttEventPublisher
{
    [Header("Topic Configuration")]
    [SerializeField] private string topicEventPublish = "events/";
    [SerializeField] private string topicScoreRequest = "request/points";

    public void PublishEvent(TrainingEvent evt)
    {
        if (evt == null)
        {
            Debug.LogWarning("[MqttEventPublisher] Cannot publish null event");
            return;
        }

        string json = JsonUtility.ToJson(evt);
        MqttApplicationManager.Instance?.Publish(topicEventPublish, json);
    }

    public void PublishScoreRequest(ScoreRequestMessage request)
    {
        if (request == null)
        {
            Debug.LogWarning("[MqttEventPublisher] Cannot publish null score request");
            return;
        }

        string json = JsonUtility.ToJson(request);
        MqttApplicationManager.Instance?.Publish(topicScoreRequest, json);
    }
}
