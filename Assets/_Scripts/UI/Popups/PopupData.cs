// Payloads handed to popups by controllers, so popups never read game state themselves.
public class SettingsPopupData
{
    public readonly float SfxVolume;
    public readonly float MusicVolume;

    public SettingsPopupData(float sfxVolume, float musicVolume)
    {
        SfxVolume = sfxVolume;
        MusicVolume = musicVolume;
    }
}

public class PausePopupData
{
    public readonly int HighScore;

    public PausePopupData(int highScore) => HighScore = highScore;
}

public class GameOverPopupData
{
    public readonly int Score;
    public readonly int Best;
    public readonly bool IsNewBest;

    public GameOverPopupData(int score, int best, bool isNewBest)
    {
        Score = score;
        Best = best;
        IsNewBest = isNewBest;
    }
}
