using UnityEngine;
using UnityEngine.SceneManagement;

public class StartUI : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The exact name or index of the game scene to load.")]
    public string gameSceneName = "MainScene"; // Update this to your actual game scene name

    /// <summary>
    /// Call this from your Start/Play button's OnClick event.
    /// </summary>
    public void StartGame()
    {
        Debug.Log($"Starting Game! Loading Scene: {gameSceneName}...");
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Call this from your Exit/Quit button's OnClick event.
    /// </summary>
    public void ExitGame()
    {
        Debug.Log("Exiting Game...");
        
        // Quits the application
        Application.Quit();

#if UNITY_EDITOR
        // Stops play mode if you are running inside the Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
