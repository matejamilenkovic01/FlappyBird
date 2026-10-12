using System;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PipePair : MonoBehaviour, IPoolable
{
    [SerializeField] private Transform _top;
    [SerializeField] private Transform _bottom;
    [SerializeField] private BoxCollider2D _scoreZone;
    [SerializeField] private Transform[] _pulseTargets;
    [SerializeField] private float _pulseScale = 1.06f;

    private Rigidbody2D _rigidbody;
    private LevelScroller _scroller;
    private Action<PipePair> _release;
    private Action<PipePair> _passed;
    private float _despawnX;
    private bool _hasScored;

    public int ScoreValue { get; private set; }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 position, float gapSize, int scoreValue, LevelScroller scroller, float despawnX,
        Action<PipePair> passed, Action<PipePair> release)
    {
        transform.position = position;
        _rigidbody.position = position;
        _top.localPosition = new Vector3(0f, gapSize * 0.5f, 0f);
        _bottom.localPosition = new Vector3(0f, -gapSize * 0.5f, 0f);
        _scoreZone.size = new Vector2(_scoreZone.size.x, gapSize);

        ScoreValue = scoreValue;
        _scroller = scroller;
        _despawnX = despawnX;
        _passed = passed;
        _release = release;
    }

    public void OnSpawned()
    {
        _hasScored = false;

        foreach (Transform target in _pulseTargets)
        {
            target.localScale = Vector3.one;
            target.DOScale(_pulseScale, 0.45f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
        }
    }

    public void OnDespawned()
    {
        foreach (Transform target in _pulseTargets)
            target.DOKill();
    }

    private void FixedUpdate()
    {
        Vector2 next = _rigidbody.position + Vector2.left * (_scroller.CurrentSpeed * Time.fixedDeltaTime);
        _rigidbody.MovePosition(next);

        if (next.x < _despawnX)
            _release?.Invoke(this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasScored || !other.TryGetComponent(out Bird _))
            return;

        _hasScored = true;
        _passed?.Invoke(this);
    }
}
