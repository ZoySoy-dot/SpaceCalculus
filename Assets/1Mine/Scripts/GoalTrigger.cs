using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalTrigger : MonoBehaviour
{
    [Tooltip("Name of the scene to load after reaching the goal")]
    public string mainMenuSceneName = "MainMenu";

    [Tooltip("Optional delay before loading scene (in seconds)")]
    public float delayBeforeReturn = 1f;

    private bool hasFinished = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasFinished) return;

        if (other.CompareTag("Goal"))
        {
            hasFinished = true;
            Debug.Log("🎉 Goal reached! Finishing level...");

            // Optional: add animation or sound here

            // Delay before returning to main menu
            Invoke(nameof(LoadMainMenu), delayBeforeReturn);
        }
    }

    private void LoadMainMenu()
    {
        Debug.Log("🏁 Loading Main Menu...");
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
