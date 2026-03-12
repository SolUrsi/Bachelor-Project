using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Development-only helper: press Space to add 10 points and publish a SCORE_UPDATED
/// event to the MQTT broker, so the event pipeline can be verified end-to-end in the
/// Unity Editor without playing through the full scenario.
///
/// The keyboard check is compiled out in production builds to ensure no spurious
/// SCORE_UPDATED events ever reach the broker and pollute the real session history.
/// </summary>
public class ScoreTestInput : MonoBehaviour
{
    [SerializeField] private TrainingScenarioController controller;

    private void Update()
    {
#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (controller != null && controller.ScoreManager != null)
            {
                controller.ScoreManager.AddPoints(10, "Manual test", "TEST_OBJECT");
                Debug.Log("[ScoreTestInput] Space pressed: +10 local points (non-authoritative)");
            }
            else
            {
                Debug.LogWarning("[ScoreTestInput] Controller or ScoreManager is missing.");
            }
        }
#endif
    }
}