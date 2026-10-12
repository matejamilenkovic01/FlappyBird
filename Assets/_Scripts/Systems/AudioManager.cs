using System;
using UnityEngine;

public class AudioManager : SingletonPersistent<AudioManager>
{
    private const float DefaultSfxVolume = 0.8f;
    private const float DefaultMusicVolume = 0.5f;
    private const float PausedMusicFactor = 0.5f;

    [SerializeField] private SoundData[] _sounds;
    [SerializeField] private AudioClip _musicClip;
    [SerializeField] private AudioSource _sfxSource;
    [SerializeField] private AudioSource _musicSource;

    private float _sfxVolume;
    private float _musicVolume;
    private bool _isPaused;

    public float SfxVolume => _sfxVolume;
    public float MusicVolume => _musicVolume;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        _sfxVolume = PlayerPrefs.GetFloat(SaveKeys.SfxVolume, DefaultSfxVolume);
        _musicVolume = PlayerPrefs.GetFloat(SaveKeys.MusicVolume, DefaultMusicVolume);
        ApplyVolumes();

        _musicSource.clip = _musicClip;
        _musicSource.loop = true;
        _musicSource.Play();
    }

    private void OnEnable()
    {
        GameEvents.OnSfxVolumeChanged += HandleSfxVolumeChanged;
        GameEvents.OnMusicVolumeChanged += HandleMusicVolumeChanged;
        GameEvents.OnButtonClicked += HandleButtonClicked;
        GameEvents.OnBirdFlapped += HandleBirdFlapped;
        GameEvents.OnScoreChanged += HandleScoreChanged;
        GameEvents.OnBirdDied += HandleBirdDied;
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnSfxVolumeChanged -= HandleSfxVolumeChanged;
        GameEvents.OnMusicVolumeChanged -= HandleMusicVolumeChanged;
        GameEvents.OnButtonClicked -= HandleButtonClicked;
        GameEvents.OnBirdFlapped -= HandleBirdFlapped;
        GameEvents.OnScoreChanged -= HandleScoreChanged;
        GameEvents.OnBirdDied -= HandleBirdDied;
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveVolumes();
    }

    public void Play(SoundId id, bool varyPitch = false)
    {
        SoundData sound = Array.Find(_sounds, s => s.Id == id);
        if (sound == null || sound.Clip == null)
        {
            Debug.LogWarning($"AudioManager: no clip for {id}");
            return;
        }

        // PlayOneShot uses the source pitch, so variation is applied per call and reset right after.
        _sfxSource.pitch = varyPitch ? UnityEngine.Random.Range(0.95f, 1.05f) : 1f;
        _sfxSource.PlayOneShot(sound.Clip, sound.Volume);
    }

    public void SaveVolumes()
    {
        PlayerPrefs.SetFloat(SaveKeys.SfxVolume, _sfxVolume);
        PlayerPrefs.SetFloat(SaveKeys.MusicVolume, _musicVolume);
        PlayerPrefs.Save();
    }

    private void ApplyVolumes()
    {
        _sfxSource.volume = _sfxVolume;
        _musicSource.volume = _musicVolume * (_isPaused ? PausedMusicFactor : 1f);
    }

    private void HandleSfxVolumeChanged(float volume)
    {
        _sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SaveKeys.SfxVolume, _sfxVolume);
        ApplyVolumes();
    }

    private void HandleMusicVolumeChanged(float volume)
    {
        _musicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SaveKeys.MusicVolume, _musicVolume);
        ApplyVolumes();
    }

    private void HandleGameStateChanged(GameState state)
    {
        _isPaused = state == GameState.Paused;
        ApplyVolumes();
    }

    private void HandleButtonClicked() => Play(SoundId.Click, true);

    private void HandleBirdFlapped() => Play(SoundId.Flap, true);

    private void HandleScoreChanged(int score, int delta)
    {
        if (delta <= 0)
            return;

        Play(delta > 1 ? SoundId.Bonus : SoundId.Score);
    }

    private void HandleBirdDied() => Play(SoundId.Hit);
}

[Serializable]
public class SoundData
{
    public SoundId Id;
    public AudioClip Clip;
    [Range(0f, 1f)] public float Volume = 1f;
}

public enum SoundId
{
    Click = 0,
    Flap = 1,
    Score = 2,
    Bonus = 3,
    Hit = 4,
    Fall = 5,
    Whoosh = 6,
    PopupOpen = 7
}
