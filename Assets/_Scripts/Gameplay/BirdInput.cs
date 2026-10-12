using UnityEngine;
using UnityEngine.InputSystem;

// Keyboard only. Touch and mouse go through the UI TapCatcher, so UI elements on top of it
// (pause, score) never trigger a flap and a click never flaps twice.
public class BirdInput : MonoBehaviour
{
    private InputAction _flapAction;
    private GameState _state = GameState.Ready;

    private void Awake()
    {
        _flapAction = new InputAction("Flap", InputActionType.Button);
        _flapAction.AddBinding("<Keyboard>/space");
        _flapAction.AddBinding("<Keyboard>/w");
        _flapAction.AddBinding("<Keyboard>/upArrow");
    }

    private void OnEnable()
    {
        _flapAction.performed += HandleFlapPerformed;
        _flapAction.Enable();
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        _flapAction.performed -= HandleFlapPerformed;
        _flapAction.Disable();
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void OnDestroy() => _flapAction.Dispose();

    private void HandleGameStateChanged(GameState state) => _state = state;

    private void HandleFlapPerformed(InputAction.CallbackContext context)
    {
        if (_state == GameState.Ready || _state == GameState.Playing)
            GameEvents.RaiseFlapRequested();
    }
}
