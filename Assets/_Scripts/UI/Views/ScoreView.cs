using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class ScoreView : MonoBehaviour
{
    private const float HighlightDuration = 0.2f;

    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _highlightColor = new(0.298f, 0.851f, 0.392f); // #4CD964
    [Tooltip("Floating '+N' shown for bonus pipes (delta > 1).")]
    [SerializeField] private TMP_Text _bonusFloat;
    [SerializeField] private float _bonusFloatRise = 90f;

    private TMP_Text _text;
    private Tween _colorReset;
    private Sequence _bonusSequence;
    private Vector2 _bonusRestPosition;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
        _bonusRestPosition = _bonusFloat.rectTransform.anchoredPosition;
        _bonusFloat.gameObject.SetActive(false);
    }

    private void OnEnable() => GameEvents.OnScoreChanged += HandleScoreChanged;

    private void OnDisable() => GameEvents.OnScoreChanged -= HandleScoreChanged;

    private void HandleScoreChanged(int score, int delta)
    {
        _text.text = score.ToString();
        if (delta <= 0)
            return;

        transform.DOKill(true);
        _colorReset?.Kill();

        transform.DOPunchScale(Vector3.one * 0.35f, 0.25f, 8, 0.5f).SetLink(gameObject);
        _text.color = _highlightColor;
        _colorReset = DOVirtual.DelayedCall(HighlightDuration, () => _text.color = _normalColor).SetLink(gameObject);

        if (delta > 1)
            PlayBonusFloat(delta);
    }

    private void PlayBonusFloat(int delta)
    {
        _bonusSequence?.Kill();
        RectTransform rect = _bonusFloat.rectTransform;
        _bonusFloat.text = $"+{delta}";
        _bonusFloat.alpha = 1f;
        rect.anchoredPosition = _bonusRestPosition;
        rect.localScale = Vector3.one * 0.6f;
        _bonusFloat.gameObject.SetActive(true);

        _bonusSequence = DOTween.Sequence()
            .Join(rect.DOScale(1f, 0.2f).SetEase(Ease.OutBack))
            .Join(rect.DOAnchorPosY(_bonusRestPosition.y + _bonusFloatRise, 0.7f).SetEase(Ease.OutCubic))
            .Insert(0.35f, _bonusFloat.DOFade(0f, 0.35f))
            .OnComplete(() => _bonusFloat.gameObject.SetActive(false))
            .SetLink(gameObject);
    }
}
