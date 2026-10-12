using DG.Tweening;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class ScoreView : MonoBehaviour
{
    private const float HighlightDuration = 0.2f;

    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _highlightColor = new(0.298f, 0.851f, 0.392f); // #4CD964

    private TMP_Text _text;
    private Tween _colorReset;

    private void Awake() => _text = GetComponent<TMP_Text>();

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
    }
}
