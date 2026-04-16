using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseUI : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The exact name or index of the start scene to load.")]
    public string gameSceneName = "StartScene";

    /// <summary>
    /// Call this from your Resume button's OnClick event.
    /// </summary>
    public void ResumeGame()
    {
        PauseManager.TogglePause(false);
        Debug.Log("Game Resumed");
        Destroy(gameObject); // Destroys the UI canvas
    }

    /// <summary>
    /// Call this from your Quit button's OnClick event.
    /// </summary>
    public void QuitGame()
    {
        PauseManager.TogglePause(false);
        SceneManager.LoadScene(gameSceneName);
        Debug.Log("Game Quit");
    }
}
