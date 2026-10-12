using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Esc in the Editor and the Android Back button both arrive as Keyboard.escapeKey.
public class InputRouter : SingletonPersistent<InputRouter>
{
    private const string MainSceneName = "Main";

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            return;

        HandleBack();
    }

    private void HandleBack()
    {
        if (SceneLoader.Instance != null && SceneLoader.Instance.IsLoading)
            return;

        // The top popup decides what Back means (close, resume, or ignore).
        if (PopupManager.Instance != null && PopupManager.Instance.HideTop(fromBack: true))
            return;

        if (SceneManager.GetActiveScene().name == MainSceneName)
            GameEvents.RaisePauseRequested();
    }
}
