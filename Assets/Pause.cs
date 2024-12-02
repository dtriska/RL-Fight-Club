using UnityEngine;
using UnityEngine.UI;

public class PauseButton : MonoBehaviour
{
    public Button pauseButton;
    public Text buttonText;

    private bool isPaused = false;

    private void Start()
    {
        // Set initial button text
        UpdateButtonText();
        // Add listener to the button to toggle pause on click
        pauseButton.onClick.AddListener(TogglePause);
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0 : 1;
        UpdateButtonText();
    }

    private void UpdateButtonText()
    {
        buttonText.text = isPaused ? "Resume" : "Pause";
    }
}