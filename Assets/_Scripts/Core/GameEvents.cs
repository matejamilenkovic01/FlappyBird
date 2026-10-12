using System;
using UnityEngine;

// Single hub for every cross-layer event. UI only talks to the game through these.
public static class GameEvents
{
    #region Menu
    public static event Action OnPlayRequested;
    public static event Action OnSettingsRequested;

    public static void RaisePlayRequested() => OnPlayRequested?.Invoke();
    public static void RaiseSettingsRequested() => OnSettingsRequested?.Invoke();
    #endregion

    #region Game flow
    public static event Action OnPauseRequested;
    public static event Action OnResumeRequested;
    public static event Action OnRestartRequested;
    public static event Action OnMenuRequested;
    public static event Action<GameState> OnGameStateChanged;

    public static void RaisePauseRequested() => OnPauseRequested?.Invoke();
    public static void RaiseResumeRequested() => OnResumeRequested?.Invoke();
    public static void RaiseRestartRequested() => OnRestartRequested?.Invoke();
    public static void RaiseMenuRequested() => OnMenuRequested?.Invoke();
    public static void RaiseGameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);
    #endregion

    #region Settings
    public static event Action<float> OnSfxVolumeChanged;
    public static event Action<float> OnMusicVolumeChanged;

    public static void RaiseSfxVolumeChanged(float volume) => OnSfxVolumeChanged?.Invoke(volume);
    public static void RaiseMusicVolumeChanged(float volume) => OnMusicVolumeChanged?.Invoke(volume);
    #endregion

    #region UI
    public static event Action OnButtonClicked;

    public static void RaiseButtonClicked() => OnButtonClicked?.Invoke();
    #endregion

    #region Gameplay
    public static event Action OnFlapRequested;
    public static event Action OnBirdFlapped;
    public static event Action<int, int> OnScoreChanged;
    public static event Action OnBirdDied;

    public static void RaiseFlapRequested() => OnFlapRequested?.Invoke();
    public static void RaiseBirdFlapped() => OnBirdFlapped?.Invoke();
    public static void RaiseScoreChanged(int score, int delta) => OnScoreChanged?.Invoke(score, delta);
    public static void RaiseBirdDied() => OnBirdDied?.Invoke();
    #endregion

    // Static events survive Play mode when domain reload is disabled, so stale subscribers are dropped here.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAll()
    {
        OnPlayRequested = null;
        OnSettingsRequested = null;
        OnPauseRequested = null;
        OnResumeRequested = null;
        OnRestartRequested = null;
        OnMenuRequested = null;
        OnGameStateChanged = null;
        OnSfxVolumeChanged = null;
        OnMusicVolumeChanged = null;
        OnButtonClicked = null;
        OnFlapRequested = null;
        OnBirdFlapped = null;
        OnScoreChanged = null;
        OnBirdDied = null;
    }
}
