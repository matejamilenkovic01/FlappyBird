using UnityEngine;

public class MenuController : MonoBehaviour
{
    private const string MainSceneName = "Main";

    private void OnEnable()
    {
        GameEvents.OnPlayRequested += HandlePlayRequested;
        GameEvents.OnSettingsRequested += HandleSettingsRequested;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayRequested -= HandlePlayRequested;
        GameEvents.OnSettingsRequested -= HandleSettingsRequested;
    }

    private void HandlePlayRequested() => SceneLoader.Instance.Load(MainSceneName);

    private void HandleSettingsRequested()
    {
        AudioManager audio = AudioManager.Instance;
        SettingsPopup popup = PopupManager.Instance.Show<SettingsPopup>(new SettingsPopupData(audio.SfxVolume, audio.MusicVolume));
        if (popup == null)
            return;

        popup.Hidden -= HandleSettingsHidden;
        popup.Hidden += HandleSettingsHidden;
    }

    private void HandleSettingsHidden(PopupBase popup)
    {
        popup.Hidden -= HandleSettingsHidden;
        AudioManager.Instance.SaveVolumes();
    }
}
