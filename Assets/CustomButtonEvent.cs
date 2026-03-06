using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class CustomButtonEvent : MonoBehaviour
{
    void Start()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        Button Case = root.Q<Button>("Case");
        Button Tutorial = root.Q<Button>("Tutorial");

        Case.clicked += OnCasePressed;
        Tutorial.clicked += OnTutorialPressed;
    }

    void OnCasePressed()
    {
        SceneManager.LoadScene("Scene 1");
    }

    void OnTutorialPressed()
    {
        SceneManager.LoadScene("Tutorial");
    }
}