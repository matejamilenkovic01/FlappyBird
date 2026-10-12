using TMPro;
using UnityEngine;

public class MainMenuView : MonoBehaviour
{
    [SerializeField] private UIButton _playButton;
    [SerializeField] private UIButton _settingsButton;
    [SerializeField] private TMP_Text _versionText;

    private void Awake() => _versionText.text = $"v{Application.version}";

    private void OnEnable()
    {
        _playButton.Clicked += GameEvents.RaisePlayRequested;
        _settingsButton.Clicked += GameEvents.RaiseSettingsRequested;
    }

    private void OnDisable()
    {
        _playButton.Clicked -= GameEvents.RaisePlayRequested;
        _settingsButton.Clicked -= GameEvents.RaiseSettingsRequested;
    }
}
