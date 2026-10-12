using TMPro;
using UnityEngine;

public class PausePopup : PopupBase
{
    [SerializeField] private TMP_Text _bestText;
    [SerializeField] private UIButton _resumeButton;
    [SerializeField] private UIButton _menuButton;

    private void OnEnable()
    {
        _resumeButton.Clicked += GameEvents.RaiseResumeRequested;
        _menuButton.Clicked += GameEvents.RaiseMenuRequested;
    }

    private void OnDisable()
    {
        _resumeButton.Clicked -= GameEvents.RaiseResumeRequested;
        _menuButton.Clicked -= GameEvents.RaiseMenuRequested;
    }

    // Back means "resume": the controller unpauses and closes this popup.
    public override void HandleBack() => GameEvents.RaiseResumeRequested();

    protected override void OnShow(object payload)
    {
        if (payload is PausePopupData data)
            _bestText.text = $"BEST: {data.HighScore}";
    }
}
