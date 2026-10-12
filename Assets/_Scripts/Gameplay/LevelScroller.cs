using UnityEngine;

// Single source of world scroll speed; pipes and scrolling loops read it every physics step.
public class LevelScroller : MonoBehaviour
{
    [SerializeField] private PipeSpawnConfig _config;

    private GameState _state = GameState.Ready;
    private int _score;

    public float CurrentSpeed { get; private set; }

    private void Awake() => UpdateSpeed();

    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        GameEvents.OnScoreChanged += HandleScoreChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        GameEvents.OnScoreChanged -= HandleScoreChanged;
    }

    private void HandleGameStateChanged(GameState state)
    {
        _state = state;
        UpdateSpeed();
    }

    private void HandleScoreChanged(int score, int delta)
    {
        _score = score;
        UpdateSpeed();
    }

    private void UpdateSpeed()
    {
        CurrentSpeed = _state switch
        {
            GameState.Ready => _config.BaseSpeed,
            GameState.Playing => _config.GetSpeed(_score),
            _ => 0f
        };
    }
}
