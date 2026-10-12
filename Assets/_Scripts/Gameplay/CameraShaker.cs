using DG.Tweening;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [SerializeField] private float _duration = 0.35f;
    [SerializeField] private float _strength = 0.25f;
    [SerializeField] private int _vibrato = 20;
    [SerializeField] private float _randomness = 90f;

    private Vector3 _restPosition;

    private void Awake() => _restPosition = transform.localPosition;

    private void OnEnable() => GameEvents.OnBirdDied += HandleBirdDied;

    private void OnDisable() => GameEvents.OnBirdDied -= HandleBirdDied;

    private void HandleBirdDied()
    {
        transform.DOKill();
        transform.localPosition = _restPosition;
        // Z stays put: shaking it would only move the camera through the sprites' depth.
        transform.DOShakePosition(_duration, new Vector3(_strength, _strength, 0f), _vibrato, _randomness)
            .SetLink(gameObject)
            .OnComplete(() => transform.localPosition = _restPosition);
    }
}
