using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameoverUI : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The exact name or index of the game scene to reload.")]
    public string restartSceneName = "MainScene";

    [Tooltip("The exact name or index of the start/menu scene to quit to.")]
    public string quitSceneName = "StartScene";

    [Header("UI Settings")]
    [Tooltip("The text to display when the game is over.")]
    public TMP_Text HeaderText;

    public void Initialize(string headerText)
    {
        HeaderText.text = headerText;
    }

    private void Start()
    {
        PauseManager.TogglePause(true);
    }

    /// <summary>
    /// Call this from your Restart button's OnClick event.
    /// </summary>
    public void RestartGame()
    {
        PauseManager.TogglePause(false);
        
        Debug.Log($"Restarting Game! Loading Scene: {restartSceneName}...");
        SceneManager.LoadScene(restartSceneName);
    }

    /// <summary>
    /// Call this from your Quit/Main Menu button's OnClick event.
    /// </summary>
    public void QuitGame()
    {
        PauseManager.TogglePause(false);

        Debug.Log($"Quitting to Main Menu! Loading Scene: {quitSceneName}...");
        SceneManager.LoadScene(quitSceneName);
    }
}
