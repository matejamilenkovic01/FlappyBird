using System.Collections;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private const float GameOverDelay = 1f;
    private const string MainSceneName = "Main";
    private const string MenuSceneName = "Menu";

    [SerializeField] private PipeSpawner _spawner;

    private int _score;
    private int _highScore;
    private GameState _stateBeforePause;

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
        GameEvents.OnPauseRequested += HandlePauseRequested;
        GameEvents.OnResumeRequested += HandleResumeRequested;
        GameEvents.OnRestartRequested += HandleRestartRequested;
        GameEvents.OnMenuRequested += HandleMenuRequested;
        _spawner.PipePassed += HandlePipePassed;
    }

    private void OnDisable()
    {
        GameEvents.OnFlapRequested -= HandleFlapRequested;
        GameEvents.OnBirdDied -= HandleBirdDied;
        GameEvents.OnPauseRequested -= HandlePauseRequested;
        GameEvents.OnResumeRequested -= HandleResumeRequested;
        GameEvents.OnRestartRequested -= HandleRestartRequested;
        GameEvents.OnMenuRequested -= HandleMenuRequested;
        _spawner.PipePassed -= HandlePipePassed;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        SetState(GameState.Ready);
        GameEvents.RaiseScoreChanged(_score, 0);
    }

    // Leaving the app mid-run (home button, call) pauses instead of letting the bird die unseen.
    private void OnApplicationPause(bool paused)
    {
        if (paused && State == GameState.Playing)
            HandlePauseRequested();
    }

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

    private void HandlePauseRequested()
    {
        if (State != GameState.Playing && State != GameState.Ready)
            return;

        _stateBeforePause = State;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
        PopupManager.Instance.Show<PausePopup>(new PausePopupData(_highScore));
    }

    private void HandleResumeRequested()
    {
        if (State != GameState.Paused)
            return;

        PopupManager.Instance.Hide<PausePopup>();
        Time.timeScale = 1f;
        SetState(_stateBeforePause);
    }

    private void HandleRestartRequested() => SceneLoader.Instance.Load(MainSceneName);

    private void HandleMenuRequested()
    {
        Time.timeScale = 1f;
        SceneLoader.Instance.Load(MenuSceneName);
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

        bool isNewBest = _score > _highScore;
        if (isNewBest)
        {
            _highScore = _score;
            PlayerPrefs.SetInt(SaveKeys.HighScore, _highScore);
            PlayerPrefs.Save();
        }

        SetState(GameState.GameOver);
        PopupManager.Instance.Show<GameOverPopup>(new GameOverPopupData(_score, _highScore, isNewBest));
    }
}
