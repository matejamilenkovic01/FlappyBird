using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class GetReadyView : MonoBehaviour
{
    [SerializeField] private RectTransform _tapHint;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = false;
    }

    private void OnEnable() => GameEvents.OnGameStateChanged += HandleGameStateChanged;

    private void OnDisable() => GameEvents.OnGameStateChanged -= HandleGameStateChanged;

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.Ready)
        {
            _canvasGroup.DOKill();
            _canvasGroup.alpha = 1f;
            _tapHint.DOKill();
            _tapHint.localScale = Vector3.one;
            _tapHint.DOScale(0.85f, 0.45f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
        }
        else if (state == GameState.Playing && _canvasGroup.alpha > 0f)
        {
            _canvasGroup.DOFade(0f, 0.25f).SetLink(gameObject)
                .OnComplete(() => _tapHint.DOKill());
        }
    }
}
