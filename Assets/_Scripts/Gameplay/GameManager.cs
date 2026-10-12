using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#endif

public class GameManager : Singleton<GameManager>
{
    private const float GameOverDelay = 1f;

    [SerializeField] private PipeSpawner _spawner;

    private int _score;
    private int _highScore;

    public GameState State { get; private set; } = GameState.Ready;
    public int Score => _score;
    public int HighScore => _highScore;

    protected override void Awake()
    {
        base.Awake();
        _highScore = PlayerPrefs.GetInt(SaveKeys.HighScore, 0);
    }

    private void OnEnable()
    {
        GameEvents.OnFlapRequested += HandleFlapRequested;
        GameEvents.OnBirdDied += HandleBirdDied;
        _spawner.PipePassed += HandlePipePassed;
    }

    private void OnDisable()
    {
        GameEvents.OnFlapRequested -= HandleFlapRequested;
        GameEvents.OnBirdDied -= HandleBirdDied;
        _spawner.PipePassed -= HandlePipePassed;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        SetState(GameState.Ready);
        GameEvents.RaiseScoreChanged(_score, 0);
    }

#if UNITY_EDITOR
    // Temporary until the Game Over popup exists (removed in the screens & flow commit).
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
#endif

    private void SetState(GameState state)
    {
        State = state;
        GameEvents.RaiseGameStateChanged(state);
    }

    private void HandleFlapRequested()
    {
        if (State == GameState.Ready)
            SetState(GameState.Playing);
    }

    private void HandlePipePassed(int value)
    {
        if (State != GameState.Playing)
            return;

        _score += value;
        GameEvents.RaiseScoreChanged(_score, value);
    }

    private void HandleBirdDied()
    {
        if (State != GameState.Playing && State != GameState.Ready)
            return;

        SetState(GameState.Dead);
        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSecondsRealtime(GameOverDelay);

        if (_score > _highScore)
        {
            _highScore = _score;
            PlayerPrefs.SetInt(SaveKeys.HighScore, _highScore);
            PlayerPrefs.Save();
        }

        SetState(GameState.GameOver);
    }
}
