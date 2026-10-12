using DG.Tweening;
using TMPro;
using UnityEngine;

public class GameOverPopup : PopupBase
{
    private const float CountDuration = 0.5f;
    private const float CountDelay = 0.25f;

    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _bestText;
    [SerializeField] private RectTransform _newBadge;
    [SerializeField] private UIButton _restartButton;
    [SerializeField] private UIButton _menuButton;

    private Tween _countTween;

    private void OnEnable()
    {
        _restartButton.Clicked += GameEvents.RaiseRestartRequested;
        _menuButton.Clicked += GameEvents.RaiseMenuRequested;
    }

    private void OnDisable()
    {
        _restartButton.Clicked -= GameEvents.RaiseRestartRequested;
        _menuButton.Clicked -= GameEvents.RaiseMenuRequested;
    }

    protected override void OnShow(object payload)
    {
        if (payload is not GameOverPopupData data)
            return;

        _bestText.text = data.Best.ToString();
        _scoreText.text = "0";

        _countTween?.Kill();
        int shown = 0;
        _countTween = DOTween.To(() => shown, value => { shown = value; _scoreText.text = value.ToString(); }, data.Score, CountDuration)
            .SetDelay(CountDelay)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .SetLink(gameObject);

        _newBadge.DOKill();
        _newBadge.gameObject.SetActive(data.IsNewBest);
        if (data.IsNewBest)
        {
            _newBadge.localScale = Vector3.zero;
            _newBadge.DOScale(1f, 0.35f).SetDelay(CountDelay + CountDuration).SetEase(Ease.OutBack, 3f)
                .SetUpdate(true).SetLink(gameObject);
        }
    }
}
