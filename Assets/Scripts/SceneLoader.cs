using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to any GameObject in a scene (e.g., the Canvas or an empty manager).
/// Wire up buttons to call LoadScene() with the scene name.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    /// <summary>
    /// Load a scene by name. Assign this to button OnClick events.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Reload the current scene.
    /// </summary>
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Quit the application.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quit requested");
        Application.Quit();
    }
}