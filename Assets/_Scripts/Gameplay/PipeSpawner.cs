using System;
using System.Collections.Generic;
using UnityEngine;

// Pipes are recycled like rows in a virtualized scroll list: a small, constant set of objects is
// refilled with new data (gap position, type) as it scrolls back in, instead of Instantiate/Destroy per pipe.
public class PipeSpawner : MonoBehaviour
{
    private const float SpawnMargin = 1f;

    [SerializeField] private PipeSpawnConfig _config;
    [SerializeField] private LevelScroller _scroller;
    [SerializeField] private Camera _camera;

    private readonly Dictionary<PipeTypeEntry, ComponentPool<PipePair>> _pools = new();
    private readonly Dictionary<PipePair, ComponentPool<PipePair>> _owners = new();
    private bool _isSpawning;
    private float _distanceSinceSpawn;
    private float _lastGapCenterY;
    private int _spawnedCount;
    private float _spawnX;
    private float _despawnX;

    public event Action<int> PipePassed;

    public int CountActive => SumCounts(true);
    public int CountInactive => SumCounts(false);

    private void Awake()
    {
        float halfWidth = _camera.orthographicSize * _camera.aspect;
        _spawnX = _camera.transform.position.x + halfWidth + SpawnMargin;
        _despawnX = _camera.transform.position.x - halfWidth - SpawnMargin;
        _lastGapCenterY = (_config.GapCenterYRange.x + _config.GapCenterYRange.y) * 0.5f;

        foreach (PipeTypeEntry type in _config.PipeTypes)
        {
            if (type.Prefab != null)
                _pools[type] = new ComponentPool<PipePair>(type.Prefab, transform, _config.PrewarmPerType);
        }
    }

    private void OnEnable() => GameEvents.OnGameStateChanged += HandleGameStateChanged;

    private void OnDisable() => GameEvents.OnGameStateChanged -= HandleGameStateChanged;

    private void FixedUpdate()
    {
        if (!_isSpawning)
            return;

        _distanceSinceSpawn += _scroller.CurrentSpeed * Time.fixedDeltaTime;
        if (_distanceSinceSpawn < _config.SpawnDistance)
            return;

        _distanceSinceSpawn -= _config.SpawnDistance;
        // Carry the overshoot into the spawn position so spacing stays exact at any speed.
        Spawn(_spawnX - _distanceSinceSpawn);
    }

    private void HandleGameStateChanged(GameState state)
    {
        bool wasSpawning = _isSpawning;
        _isSpawning = state == GameState.Playing;

        // First pipe appears shortly after the first flap rather than a full spacing later.
        if (_isSpawning && !wasSpawning && CountActive == 0)
            _distanceSinceSpawn = _config.SpawnDistance * 0.5f;
    }

    private void Spawn(float x)
    {
        PipeTypeEntry type = _config.PickType();
        if (type == null || !_pools.TryGetValue(type, out ComponentPool<PipePair> pool))
            return;

        float maxDelta = _config.GetMaxGapCenterDelta(_spawnedCount);
        float min = Mathf.Max(_config.GapCenterYRange.x, _lastGapCenterY - maxDelta);
        float max = Mathf.Min(_config.GapCenterYRange.y, _lastGapCenterY + maxDelta);
        _lastGapCenterY = UnityEngine.Random.Range(min, max);

        PipePair pipe = pool.Get();
        _owners[pipe] = pool;
        pipe.Init(new Vector2(x, _lastGapCenterY), _config.GetGapSize(_spawnedCount), type.ScoreValue, _scroller, _despawnX,
            HandlePipePassed, Release);
        _spawnedCount++;
    }

    private void Release(PipePair pipe)
    {
        if (_owners.TryGetValue(pipe, out ComponentPool<PipePair> pool))
            pool.Release(pipe);
    }

    private void HandlePipePassed(PipePair pipe) => PipePassed?.Invoke(pipe.ScoreValue);

    private int SumCounts(bool active)
    {
        int total = 0;
        foreach (ComponentPool<PipePair> pool in _pools.Values)
            total += active ? pool.CountActive : pool.CountInactive;
        return total;
    }
}
