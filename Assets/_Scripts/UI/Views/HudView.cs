using DG.Tweening;
using UnityEngine;

public class HudView : MonoBehaviour
{
    [SerializeField] private UIButton _pauseButton;

    private void OnEnable()
    {
        _pauseButton.Clicked += GameEvents.RaisePauseRequested;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        _pauseButton.Clicked -= GameEvents.RaisePauseRequested;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState state)
    {
        bool canPause = state == GameState.Ready || state == GameState.Playing || state == GameState.Paused;
        _pauseButton.Interactable = canPause;

        Transform pause = _pauseButton.transform;
        pause.DOKill();
        if (canPause)
        {
            pause.gameObject.SetActive(true);
            pause.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(pause.gameObject);
        }
        else
        {
            pause.DOScale(0f, 0.15f).SetEase(Ease.InBack).SetUpdate(true).SetLink(pause.gameObject)
                .OnComplete(() => pause.gameObject.SetActive(false));
        }
    }
}
