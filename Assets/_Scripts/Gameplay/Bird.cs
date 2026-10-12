using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bird : MonoBehaviour
{
    [SerializeField] private Transform _visual;
    [SerializeField] private Camera _camera;
    [SerializeField] private float _flapVelocity = 6f;
    [SerializeField] private float _topMargin = 0.35f;

    [Header("Rotation")]
    [SerializeField] private float _flapAngle = 25f;
    [SerializeField] private float _diveAngle = -80f;
    [SerializeField] private float _diveVelocityThreshold = -3f;
    [SerializeField] private float _riseRotationSpeed = 600f;
    [SerializeField] private float _diveRotationSpeed = 320f;

    [Header("Idle Bob")]
    [SerializeField] private float _bobAmplitude = 0.15f;
    [SerializeField] private float _bobFrequency = 1.2f;

    [Header("After Death")]
    [Tooltip("Pipes are ignored once dead so the bird always drops to the ground.")]
    [SerializeField] private LayerMask _ignoreWhenDead;

    private Rigidbody2D _rigidbody;
    private GameState _state = GameState.Ready;
    private Vector2 _startPosition;
    private float _angle;
    private bool _isDead;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _startPosition = _rigidbody.position;
        _rigidbody.bodyType = RigidbodyType2D.Kinematic;
    }

    private void OnEnable()
    {
        GameEvents.OnFlapRequested += HandleFlapRequested;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnFlapRequested -= HandleFlapRequested;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void Update()
    {
        if (_state == GameState.Ready)
        {
            float offset = Mathf.Sin(Time.time * _bobFrequency * 2f * Mathf.PI) * _bobAmplitude;
            _rigidbody.MovePosition(_startPosition + Vector2.up * offset);
            return;
        }

        UpdateRotation();
    }

    private void FixedUpdate()
    {
        if (_rigidbody.bodyType != RigidbodyType2D.Dynamic)
            return;

        // Soft ceiling: the bird can't fly off screen, but touching the top isn't a death.
        float maxY = _camera.transform.position.y + _camera.orthographicSize - _topMargin;
        if (_rigidbody.position.y > maxY)
        {
            _rigidbody.position = new Vector2(_rigidbody.position.x, maxY);
            _rigidbody.linearVelocity = new Vector2(0f, Mathf.Min(_rigidbody.linearVelocity.y, 0f));
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isDead)
            return;

        _isDead = true;
        _rigidbody.excludeLayers = _ignoreWhenDead;
        _rigidbody.linearVelocity = new Vector2(0f, Mathf.Min(_rigidbody.linearVelocity.y, 0f));
        GameEvents.RaiseBirdDied();
    }

    private void HandleFlapRequested()
    {
        // Ready is accepted too: the first tap both starts the game and is the first flap,
        // regardless of whether GameManager has already switched the state.
        if (_isDead || (_state != GameState.Ready && _state != GameState.Playing))
            return;

        _rigidbody.bodyType = RigidbodyType2D.Dynamic;
        _rigidbody.linearVelocity = new Vector2(0f, _flapVelocity);
        _angle = _flapAngle;

        _visual.DOKill(true);
        _visual.DOPunchScale(new Vector3(0.12f, -0.12f, 0f), 0.2f, 6, 0.6f).SetLink(gameObject);

        GameEvents.RaiseBirdFlapped();
    }

    private void HandleGameStateChanged(GameState state)
    {
        _state = state;
        if (state == GameState.Playing)
            _rigidbody.bodyType = RigidbodyType2D.Dynamic;
    }

    private void UpdateRotation()
    {
        if (_state == GameState.Paused)
            return;

        bool diving = _rigidbody.linearVelocity.y < _diveVelocityThreshold || _isDead;
        float target = diving ? _diveAngle : _flapAngle;
        float speed = diving ? _diveRotationSpeed : _riseRotationSpeed;
        _angle = Mathf.MoveTowards(_angle, target, speed * Time.deltaTime);
        _visual.localRotation = Quaternion.Euler(0f, 0f, _angle);
    }
}
