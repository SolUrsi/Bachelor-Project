using UnityEngine;
using UnityEngine.InputSystem;

public class ScoreTestInput : MonoBehaviour
{
    [SerializeField] private TrainingScenarioController controller;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (controller != null && controller.ScoreManager != null)
            {
                controller.ScoreManager.AddPoints(10, "Manual test", "TEST_OBJECT");
                Debug.Log("Space pressed: added 10 points");
            }
            else
            {
                Debug.LogWarning("Controller or ScoreManager is missing.");
            }
        }
    }
}