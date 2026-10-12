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
        if (SceneManager.GetActiveScene().name == MainSceneName)
            GameEvents.RaisePauseRequested();
    }
}
