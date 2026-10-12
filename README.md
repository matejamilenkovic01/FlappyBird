# Flappy Bird

Small Flappy Bird clone for Android (portrait), built with Unity 6.3 (6000.3.9f1), URP 2D, Input System and DOTween.

## Controls

| | Editor | Android |
|---|---|---|
| Flap | Space / W / ↑, or left click on the game area | Tap |
| Pause / close popup | Esc | Back button |
| Score tooltip | Click the score | Tap the score |

## Build the APK

Requires the Android module (SDK, NDK, OpenJDK) installed with the Editor.

- In the Editor: **Tools → Flappy → Build Android APK**
- From the CLI, with the Editor open: `unity command eval --detach -- --code 'return AndroidBuilder.Build();' --timeout 1800000`, then `unity job wait <job-id>`

Output: `Builds/Android/FlappyBird.apk` (release, IL2CPP ARM64, signed with the Unity debug keystore — fine for sideloading).

```bash
adb install -r Builds/Android/FlappyBird.apk
```

## Tuning

All gameplay numbers live in one ScriptableObject: `Assets/_ScriptableObjects/PipeSpawnConfig.asset`
— pipe types and spawn weights (Normal 0.85 / Bonus 0.15, bonus gives +2), pipe spacing, gap size and
range, speed curve (`BaseSpeed + SpeedPerPoint * score`, capped at `MaxSpeed`) and the warm-up for the
first pipes. Bird feel (flap velocity, gravity, tilt) is on the `Bird` prefab.

## Structure

```
Assets/
  _Scenes/              Menu (build index 0), Main (1)
  _Scripts/
    Core/               Singleton, GameEvents (all cross-layer events), ComponentPool, WeightedRandom, Bootstrap, IdleMotion
    Systems/            AudioManager, HapticsService, SceneLoader, InputRouter (persistent, in Resources/Systems.prefab)
    Gameplay/           GameManager, Bird, PipeSpawner, PipePair, LevelScroller, ScrollingLoop, CameraShaker
    Menu/               MenuController
    UI/                 UIButton, popups (PopupBase + Settings/Pause/GameOver), tooltips, views
    Editor/Builders/    code that builds scenes, prefabs, generated art and the APK
  _Prefabs/             Gameplay, UI (UIButton + variants), Popups
  _Art, _Audio/         Kenney CC0 assets (see CREDITS.md) + generated pipes and music
```

Key rules:

- **UI never calls managers.** UI scripts only raise/listen to `GameEvents`, and may call `PopupManager` and
  `TooltipManager`. Controllers pass data to popups as payloads. Check:
  `grep -rnE "\b(AudioManager|GameManager|SceneLoader|HapticsService|MenuController|InputRouter)\b|PlayerPrefs" Assets/_Scripts/UI` must print nothing.
- **Pipes are pooled** (`ComponentPool<PipePair>`), spawned by distance travelled so spacing holds as speed rises.
- `Resources/Systems.prefab` is spawned by `Bootstrap` before any scene loads, so Play works from either scene.

## Builders

Scenes, prefabs and generated assets are produced by the scripts in `Assets/_Scripts/Editor/Builders`
(menu **Tools → Flappy → Builders**). They are idempotent, but **re-running one overwrites manual edits** to the
assets it builds. Order for a full rebuild: Generated Art → Art Import → Project Settings → Gameplay Prefabs →
UI Prefabs → Systems Prefab → Main Scene → Menu Scene.
