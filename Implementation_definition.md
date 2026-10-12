# Implementation Definition — Flappy Bird (Unity 6.3, Android)

Ovaj dokument je **jedini ulaz** za agenta koji pravi igru od početka do kraja.
Čitaj ga ceo pre prvog koraka. Kad nešto nije pokriveno ovde — **stani i pitaj korisnika**, ne pretpostavljaj.

---

## 0. Pravila rada za agenta

1. Radiš **inkrementalno, kroz tačno 5 commitova** (sekcija 8). Svaki commit je smislena, kompajlabilna, pokretljiva celina. Posle svakog: `git commit` + `git push` direktno na `main`.
2. Pre svakog commita prolaziš **verifikaciju** iz tog koraka. Nikad ne tvrdiš da nešto radi ako nisi proverio; ako nešto ne možeš da proveriš (npr. haptic na telefonu), napiši to eksplicitno.
3. Posle svakog commita napiši korisniku kratak izveštaj: šta je urađeno, kako je provereno, šta nije provereno, commit hash.
4. Prati stil koda iz primer projekta `/Users/matejamilenkovic/Downloads/Islands-master/Assets/_Scripts` (vidi 4.9). Taj projekat je **referenca za stil i obrasce**, ne kopiraj ga slepo — sekcija 4.1 kaže šta se zadržava, a šta menja.
5. Stani i pitaj ako: Editor nije dostupan CLI-ju, DOTween nije importovan, neki Kenney paket ne može da se skine, build pada iz razloga koji nije očigledan, ili bi rešenje kršilo pravila iz 4.3.
6. Ne diraj ništa van `~/Desktop/UnityProjects/FlappyBird` osim privremenog download foldera (5.2).

---

## 1. Cilj i obim

Jednostavna Flappy Bird igra za **Android (portrait)**, koja se igra:
- **u Editoru tastaturom/mišem** (Space / W / ↑ / levi klik = flap, Esc = pauza/back),
- **na Android telefonu dodirom** (tap = flap, sistemsko Back dugme = pauza/zatvori popup).

Art je **pragmatičan, srednji nivo** (Kenney CC0 + par generisanih sprajtova). **UX mora biti lep**: glatke animacije, jasna povratna informacija na svaki dodir, zvuk + haptic.

### Funkcionalni zahtevi (originalni, obavezni)
| # | Zahtev |
|---|---|
| R1 | Main menu sa **Play** i **Settings** dugmetom. Settings ima **dva slajdera**: Sound Effects i Background Music (odvojeno). |
| R2 | Settings je **popup**. Napraviti **generički popup** (reusable) sa clean DOTween animacijom, API `Show()` / `Hide()`. |
| R3 | Play otvara Main scenu sa **clean transition animacijom** (paneli se zatvore → load → otvore). |
| R4 | UI u Main sceni: samo **score tekst** i **pause dugme**. Pause otvara pause popup: **Resume**, **Menu**, **High score** (PlayerPrefs, perzistira posle izlaska iz igre). |
| R5 | **Svako dugme** je isti generički wrapper skript + **isti prefab**, sa **click scale animacijom** i **promenom sprite-a** na klik. |
| R6 | Na svaki klik dugmeta: **click SFX** + **blaga vibracija** (haptic). |
| R7 | Spawn stubova koristi **Object Pool** (obavezno). |
| R8 | **Različiti tipovi stubova** sa različitim verovatnoćama, sve definisano u **jednom ScriptableObject-u**. Tipovi: **Normal** i **Bonus** (zlatni, daje 2 poena). |
| R9 | Svako povećanje score-a: **pop animacija** score teksta + privremeno **zelena boja (0.2s)**. |
| R10 | Udarac u stub/zemlju: **screen shake**, **blokiran input** (mrtva ptica ne reaguje), posle **1 sekunde Game Over popup**. |
| R11 | Klik na score tokom igre otvara **tooltip** — napraviti **generički tooltip sistem** (kao u Top Eleven-u). |
| R12 | **UI ne sme da poziva singletone** (ni za čitanje podataka) — isključivo reaguje na evente. Izuzetak: Popup i Tooltip sistem smeju biti singletoni koje UI poziva. |
| R13 | Igra se builduje kao **APK** koji se može testirati na Android telefonu. |

### Odluke korisnika (dodatne)
- Game Over popup: **Score + Best (+ "NEW" oznaka) + Restart + Menu**.
- Težina: **blago ubrzanje** sa score-om do maksimuma (sve u configu).
- Art: **Kenney CC0** paketi; šta Kenney nema (klasični stubovi, muzička petlja) generiše se Editor skriptom.
- DOTween importuje **korisnik ručno** (preduslov, 2.2).
- Scene/prefabi se prave preko **Unity CLI koji upravlja otvorenim Editorom** (sekcija 3).
- Git: **direktno na `main`**, push posle svakog commita.

### Van obima
Reklame, IAP, leaderboard, lokalizacija, iOS build, Play Store potpisivanje (koristi se debug keystore).

---

## 2. Okruženje i preduslovi

### 2.1 Provereno stanje (u trenutku pisanja)
| Stvar | Vrednost |
|---|---|
| Projekat | `/Users/matejamilenkovic/Desktop/UnityProjects/FlappyBird` |
| Editor | Unity **6000.3.9f1** (6.3 LTS), arm64, `/Applications/Unity/Hub/Editor/6000.3.9f1/Unity.app` |
| Template | Universal 2D (URP 17.3, Renderer2D) |
| Input | `com.unity.inputsystem` 1.18.0, Active Input Handling = Input System (new) |
| Android moduli | OpenJDK 17, SDK platforms 34/35/36, build-tools 36.0.0, NDK r27c — instalirani |
| `aapt` | `/Applications/Unity/Hub/Editor/6000.3.9f1/PlaybackEngines/AndroidPlayer/SDK/build-tools/36.0.0/aapt` |
| `adb` | `/Applications/Unity/Hub/Editor/6000.3.9f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb` |
| Unity CLI | `~/.unity/bin/unity` (u PATH-u), ulogovan, licenca Unity Personal |
| Git | `main` prati `origin` = `git@github.com:matejamilenkovic01/FlappyBird.git` (SSH radi) |
| Build platforma | Android (već switch-ovano) |
| Trenutni Player Settings | orijentacija **AutoRotation** (mora Portrait), package name **prazan**, min API 25, ARM64 |
| Scene | samo `Assets/Scenes/SampleScene.unity` (obrisati) |
| Python | 3.9 **bez Pillow-a** → sprajtove i audio generisati iz Unity Editor C# koda, ne iz Pythona |

### 2.2 Preduslovi koje radi KORISNIK pre nego što agent krene
Agent na početku **proverava** svaki; ako nešto fali, traži od korisnika da to uradi.
1. **DOTween (free)** importovan iz Asset Store-a (Package Manager → My Assets → DOTween → Import). Zatim **Tools → Demigiant → DOTween Utility Panel → Setup DOTween**, sa uključenim modulima **UI**, **Sprite**, **Audio**, **TextMeshPro**. Provera: postoji `Assets/Plugins/Demigiant/DOTween/` i `Assets/Resources/DOTweenSettings.asset`.
2. **Unity Editor je otvoren** na FlappyBird projektu i ne prikazuje modalni dijalog.
3. Unity CLI je ažuran: `unity self-update --check`.
4. TMP Essentials su importovani (Window → TextMeshPro → Import TMP Essential Resources), ako već nisu.

---

## 3. Rad sa Unity CLI (kako agent pravi scene, prefabe i assete)

### 3.1 Setup (jednom, u commitu 1)
```bash
cd ~/Desktop/UnityProjects/FlappyBird
unity status                 # mora prikazati Editor za ovaj projekat u stanju "ready"
unity pipeline install       # dodaje com.unity.pipeline (eksperimentalan, ~0.8.0-exp) u manifest
unity command                # izlistaj komande koje Editor nudi; proveri da postoji "eval"
unity skill show             # pročitaj vodič; posebno playmode-verification-loop i integration-advanced
```
Ako `unity status` ne vidi Editor: možda tvoj sandbox skriva proces, ili je Editor u **Safe Mode** zbog compile grešaka (`unity pipeline list`). Ne improvizuj — reci korisniku i pitaj.

### 3.2 Obrazac rada: "Builder" Editor skripte + `eval` u živom Editoru
Pravljenje cele scene ili UI hijerarhije kroz desetine pojedinačnih `eval` poziva je krhko i nečitljivo. Zato:
- Sav kod koji **konstruiše** scene, prefabe, SO assete i import podešavanja ide u **Editor skripte** u `Assets/_Scripts/Editor/Builders/` (statičke metode, npr. `UIPrefabBuilder.BuildAll()`, `MainSceneBuilder.Build()`).
- Agent ih pokreće u **živom Editoru**: `unity command eval 'MainSceneBuilder.Build();'` (za duže operacije `--timeout 300` ili `--detach` + `unity job wait`).
- Builderi koriste standardne Editor API-je: `PrefabUtility.SaveAsPrefabAsset`, `EditorSceneManager.NewScene/SaveScene`, `AssetDatabase.CreateAsset`, `SerializedObject` za privatne `[SerializeField]` reference, `TextureImporter` za import podešavanja.
- Builderi su **idempotentni** (ponovno pokretanje daje isti rezultat) i pokreću se kad se prvi put pravi/menja struktura. Posle toga su scene i prefabi normalni asseti koje korisnik može ručno da menja. Napomena za korisnika: ponovno pokretanje buildera pregaziće ručne izmene u asetima koje taj builder pravi.
- Kratke provere i sitne izmene: direktno `unity command eval '...'`.
- **Nikad ne edituj ručno `.unity` / `.prefab` / `.asset` YAML** dok je Editor dostupan.

### 3.3 Verifikacija preko CLI-ja
- Kompajliranje: `unity recompile` → 0 grešaka. Ako ima grešaka, popravi pre bilo čega drugog.
- Konzola: proveri da nema `Error`/`Exception` posle Play-a (komande iz `unity command` liste ili `unity logs`).
- Play mode: `unity command editor_play`, pa po receptu iz `playmode-verification-loop.md` potvrdi da igra **stvarno napreduje** (npr. `eval` čita `Time.frameCount` / poziciju ptice u dva navrata), uradi screenshot game view-a, pa `editor_stop`. Napomena iz vodiča: Editor bez fokusa može da se zamrzne na prvom frejmu — prati recept.
- Logika: `eval` skripte za proveru (npr. 10 000 uzoraka weighted picka → ~85/15).

### 3.4 Fallback
Ako `eval` ne postoji u ovoj verziji Pipeline paketa: napravi custom komande preko `[CliCommand]` atributa (vidi `integration-advanced.md`). Ako ni to ne radi: pitaj korisnika da zatvori Editor i pokreći buildere u batchmode-u (`unity run --execute-method Klasa.Metoda`).

---

## 4. Arhitektura

### 4.1 Šta preuzimamo iz Islands primera, a šta menjamo
| Zadržavamo | Menjamo (i zašto) |
|---|---|
| `Singleton<T>` / `SingletonPersistent<T>` baza | UIManager "god object" → zaseban Popup sistem + tanki view skripti po ekranu |
| Static C# eventi, subscribe u `OnEnable` / unsubscribe u `OnDisable` | Eventi nisu razbacani po managerima nego su u jednoj statičkoj klasi `GameEvents` (UI nikad ne referencira manager klasu, ni za event) |
| SO config + cumulative weighted random (kao `AllDecorationObjectsSO`) | Dugme-po-podklasi (`PlayButton`, `QuitButton`…) → **jedan** `UIButton` wrapper + jedan prefab (R5) |
| AudioManager sa 2 AudioSource-a + enum lookup | LeanTween → **DOTween** |
| PlayerPrefs za high score | `ObjectPooler` (lookup po imenu, ne aktivira objekat) → generički `ComponentPool<T>` (4.6.7) |
| Struktura foldera sa `_` prefiksom | Hover zvuk → izbacuje se (na mobilnom nema hovera) |
| `[SerializeField] private` polja sa `_camelCase` | Volume se sada **čuva** u PlayerPrefs |

### 4.2 Struktura foldera
```
Assets/
  _Scenes/            Menu.unity, Main.unity            (build index: Menu=0, Main=1)
  _Scripts/
    Core/             Singleton.cs, GameEvents.cs, Bootstrap.cs, ComponentPool.cs, WeightedRandom.cs, SaveKeys.cs
    Systems/          AudioManager.cs, HapticsService.cs, SceneLoader.cs, InputRouter.cs
    Gameplay/         GameManager.cs, Bird.cs, BirdInput.cs, PipeSpawner.cs, PipePair.cs, PipeSpawnConfig.cs,
                      LevelScroller.cs, ScrollingLoop.cs, CameraShaker.cs, GameState.cs
    Menu/             MenuController.cs
    UI/
      Buttons/        UIButton.cs
      Popups/         PopupBase.cs, PopupManager.cs, SettingsPopup.cs, PausePopup.cs, GameOverPopup.cs, PopupData.cs
      Tooltips/       TooltipManager.cs, TooltipView.cs, TooltipTrigger.cs
      Views/          MainMenuView.cs, HudView.cs, ScoreView.cs, TapCatcher.cs, SafeArea.cs, GetReadyView.cs
    Editor/
      Builders/       ProjectSettingsBuilder.cs, ArtImportBuilder.cs, GeneratedArtBuilder.cs,
                      SystemsPrefabBuilder.cs, GameplayPrefabBuilder.cs, UIPrefabBuilder.cs,
                      MenuSceneBuilder.cs, MainSceneBuilder.cs, AndroidBuilder.cs
  _Prefabs/
    Gameplay/         Bird, PipePair_Normal, PipePair_Bonus
    UI/               UIButton (+ prefab varijante samo za labelu/ikonicu), Slider, TooltipView
    Popups/           SettingsPopup, PausePopup, GameOverPopup
  _ScriptableObjects/ PipeSpawnConfig.asset
  _Art/
    Kenney/<pack>/    samo korišćeni fajlovi + License.txt za svaki paket
    Generated/        pipe sprajtovi (normal, gold)
  _Audio/
    Kenney/<pack>/    + License.txt
    Generated/        music_loop.wav
  Resources/
    Systems.prefab    persistent sistemi (Bootstrap ga instancira)
  Plugins/Demigiant/  DOTween (importuje korisnik)
```
Bez namespace-a i bez asmdef-ova (kao u Islands), da `eval` pozivi ostanu kratki. Obriši `Assets/Scenes/SampleScene.unity`.

### 4.3 Slojevi i pravila zavisnosti (R12 — ključno)
| Sloj | Šta je | Sme | Ne sme |
|---|---|---|---|
| **UI** (`_Scripts/UI/**`) | Sve MonoBehaviour komponente na Canvas-u: dugmad, view-ovi, popupi, tooltipi, score tekst, tap catcher | Da sluša `GameEvents`, da diže `GameEvents.Raise…`, da poziva `PopupManager.Instance` i `TooltipManager.Instance`, da dobije podatke kroz `Show(payload)` popupa, da referencira druge UI komponente | Da referencira bilo koji Manager/Service/Controller ili njihov `Instance` (`AudioManager`, `GameManager`, `SceneLoader`, `HapticsService`, `MenuController`…), da čita `PlayerPrefs` |
| **Controllers** (`GameManager`, `MenuController`) | Orkestracija toka scene | Da sluša UI request evente, da zove servise i `PopupManager`, da pravi payload za popupe | Da referencira konkretne UI view-ove (osim kroz `PopupManager.Show<T>`) |
| **Systems** (`AudioManager`, `HapticsService`, `SceneLoader`, `InputRouter`, `PopupManager`, `TooltipManager`) | Persistent servisi | Da sluša evente, da čita/piše PlayerPrefs | — |
| **Gameplay** (`Bird`, `PipeSpawner`, …) | Svet igre | Da diže/sluša evente, da prima reference preko inspektora | Da zna za UI |

**Kako UI dobija podatke bez singletona:**
- **Stanje koje se menja** (score, game state) → UI sluša event (`OnScoreChanged`, `OnGameStateChanged`).
- **Podaci za popup** (volumeni, high score) → Controller ih pročita iz servisa i prosledi kao payload: `PopupManager.Instance.Show<PausePopup>(new PausePopupData(highScore))`. Popup ih samo prikaže.
- **Akcije korisnika** → UI diže request event (`GameEvents.RaisePauseRequested()`), a Controller reaguje.

Automatska provera pre svakog commita (mora vratiti prazno):
```bash
grep -rnE "\b(AudioManager|GameManager|SceneLoader|HapticsService|MenuController|InputRouter)\b|PlayerPrefs" Assets/_Scripts/UI
```

### 4.4 `GameEvents` (statička klasa, `Core/GameEvents.cs`)
Za svaki event: `public static event Action<...> OnX;` + `public static void RaiseX(...) => OnX?.Invoke(...);`.
Obavezan reset statičkih evenata za slučaj da je domain reload isključen:
`[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetAll()` postavlja sve evente na `null`.

| Event | Payload | Ko diže | Ko sluša |
|---|---|---|---|
| `OnPlayRequested` | — | MainMenuView | MenuController |
| `OnSettingsRequested` | — | MainMenuView | MenuController |
| `OnPauseRequested` | — | HudView, InputRouter (Back) | GameManager |
| `OnResumeRequested` | — | PausePopup | GameManager |
| `OnRestartRequested` | — | GameOverPopup | GameManager |
| `OnMenuRequested` | — | PausePopup, GameOverPopup | GameManager |
| `OnSfxVolumeChanged` | `float` 0..1 | SettingsPopup | AudioManager |
| `OnMusicVolumeChanged` | `float` 0..1 | SettingsPopup | AudioManager |
| `OnButtonClicked` | — | UIButton (svako) | AudioManager (Click), HapticsService (light) |
| `OnFlapRequested` | — | TapCatcher (dodir), BirdInput (tastatura/miš) | GameManager (Ready → Playing), Bird |
| `OnBirdFlapped` | — | Bird | AudioManager (Flap) |
| `OnScoreChanged` | `int score, int delta` | GameManager | ScoreView, AudioManager (Score/Bonus po delti) |
| `OnGameStateChanged` | `GameState` | GameManager | HudView, GetReadyView, Bird, PipeSpawner, LevelScroller, BirdInput |
| `OnBirdDied` | — | Bird | GameManager, CameraShaker, AudioManager (Hit), HapticsService (strong) |

### 4.5 Bootstrap i persistent sistemi
- `Resources/Systems.prefab` sadrži: `AudioManager`, `HapticsService`, `SceneLoader` (+ transition canvas), `InputRouter`, `PopupManager` (+ popup canvas), `TooltipManager` (+ tooltip canvas), `EventSystem` (sa `InputSystemUIInputModule`).
- `Bootstrap.cs`: `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` → ako `Systems` još ne postoji, `Instantiate(Resources.Load("Systems"))` + `DontDestroyOnLoad`. Zato **Play radi iz bilo koje scene** u Editoru (i Main direktno).
- Scene **nemaju** sopstveni EventSystem (sprečava duplikat).
- Dozvoljeni singletoni: `AudioManager`, `HapticsService`, `SceneLoader`, `InputRouter`, `PopupManager`, `TooltipManager` (svi `SingletonPersistent`), plus `GameManager` (`Singleton`, samo u sceni). UI sme da zove **samo** `PopupManager` i `TooltipManager`.

### 4.6 Specifikacija sistema

#### 4.6.1 `Singleton<T>` / `SingletonPersistent<T>`
Kopiraj iz Islands, uz jednu popravku: `Singleton<T>.Awake` uništava duplikat ako `Instance` već postoji i nije `this`. Ukloni `Destroy` iz `OnApplicationQuit`.

#### 4.6.2 `UIButton` (R5, R6)
Jedan skript, jedan prefab `_Prefabs/UI/UIButton.prefab` (Image + `UIButton` + child TMP label + opcioni child icon Image). Varijante (Play, Settings, Resume, Menu, Restart, Pause) su **prefab varijante** koje menjaju samo labelu, ikonicu i veličinu, **ne skript**.
- Implementira `IPointerDownHandler`, `IPointerUpHandler`, `IPointerClickHandler`, `IPointerExitHandler`.
- Polja: `_normalSprite`, `_pressedSprite`, `_pressedScale = 0.92f`, `_interactable`.
- Down: sprite → pressed, `DOScale(_pressedScale, 0.08f).SetEase(Ease.OutQuad)`; label/ikonica se pomeri par px naniže (Kenney "depth" dugmad).
- Up/Exit: sprite → normal, `DOScale(1f, 0.2f).SetEase(Ease.OutBack)`.
- Click: `GameEvents.RaiseButtonClicked()` pa `Clicked?.Invoke()` (`public event Action Clicked`).
- Svi tweenovi `.SetUpdate(true)` (rade i dok je `timeScale = 0`) i `.SetLink(gameObject)`; prethodni tween na istom targetu se kill-uje.
- Ne koristi Unity `Button` komponentu (izbegava se dupla logika tranzicija).
- Ponašanje se kači iz view skripti: `_playButton.Clicked += GameEvents.RaisePlayRequested;` (u `OnEnable`, odjava u `OnDisable`).

#### 4.6.3 Popup sistem (R2)
**`PopupBase`** (abstract, MonoBehaviour), root ima `CanvasGroup`; deca su `_dim` (Image, crna, alpha 0.6, full screen, blokira raycast) i `_panel` (RectTransform).
- `public void Show(object payload = null)`: `SetActive(true)`, `OnShow(payload)`, `interactable = false`; dim alpha 0→1 (0.2s); panel scale 0.85→1 + alpha 0→1 (0.3s, `OutBack`); na kraju `interactable = true`.
- `public void Hide()`: `interactable = false`; panel scale →0.9 + alpha →0 (0.2s, `InBack`); dim →0 (0.2s); `OnComplete` → `SetActive(false)`, `OnHidden()`, event `Hidden`.
- `protected virtual void OnShow(object payload)`, `protected virtual void OnHidden()`.
- `[SerializeField] bool _closeOnBack = true`, `[SerializeField] bool _closeOnDimClick`.
- Svi tweenovi `SetUpdate(true)`; dupli Show/Hide tokom animacije je bezbedan (kill + nastavak od trenutnog stanja).

**`PopupManager`** (`SingletonPersistent`) — vlasnik popup canvasa (sorting order 100).
- `[SerializeField] PopupBase[] _popupPrefabs`; instancira lenjo, kešira instancu po tipu.
- `T Show<T>(object payload = null) where T : PopupBase` — push na stek.
- `void Hide<T>()`, `bool HideTop()` (poštuje `_closeOnBack` kad dolazi sa Back-a), `bool IsAnyOpen`.
- Popupi su **reuse-ovani** na 3 mesta: Settings (Menu), Pause (Main), GameOver (Main).

Konkretni popupi:
- `SettingsPopup` — payload `SettingsPopupData(float sfx, float music)`; dva slajdera (Kenney UI slider sprajtovi), na `onValueChanged` diže `OnSfxVolumeChanged` / `OnMusicVolumeChanged`; postavljanje početnih vrednosti koristi `SetValueWithoutNotify`; dugme "Close".
- `PausePopup` — payload `PausePopupData(int highScore)`; "BEST: X", dugmad Resume, Menu; `_closeOnBack = true` (Back = Resume, kroz `OnResumeRequested`).
- `GameOverPopup` — payload `GameOverPopupData(int score, int best, bool isNewBest)`; score se odbrojava 0→score (0.5s, `DOTween.To`), "NEW" bedž pop-uje ako je novi rekord; dugmad Restart, Menu; `_closeOnBack = false`.

#### 4.6.4 Tooltip sistem (R11)
Cilj: bilo koji UI element dobije tooltip dodavanjem jedne komponente.
- **`TooltipTrigger`** (UI): `[SerializeField, TextArea] string _text`, `TooltipSide _preferredSide = Below`; na `IPointerClickHandler` → `TooltipManager.Instance.Show(_text, (RectTransform)transform, _preferredSide)`. Ponovni klik na isti trigger zatvara tooltip (toggle).
- **`TooltipManager`** (`SingletonPersistent`, tooltip canvas sorting order 200): jedna pooled `TooltipView` instanca.
  - `Show(string text, RectTransform anchor, TooltipSide side)`, `Hide()`.
  - Pozicioniranje: world corners anchora → lokalne koordinate tooltip canvasa; postavi ispod/iznad sa strelicom koja pokazuje na centar anchora; **clamp** horizontalno unutar safe area (strelica ostaje na anchoru); ako nema mesta na preferiranoj strani → flip.
  - Animacija: scale 0.8→1 + fade 0→1 (0.15s, `OutBack`), hide 0.1s; `SetUpdate(true)`.
  - Zatvaranje: posle 2.5s, ili na **sledeći pritisak bilo gde** (globalno praćenje pointer press-a preko Input System-a, **bez** blokiranja tapa — igra se ne prekida), ili na promenu scene.
- **`TooltipView`**: pozadina (Kenney panel, 9-slice), TMP tekst sa auto-size po širini (max širina 600 ref px), strelica.
- Upotreba u igri: `TooltipTrigger` na score tekstu, tekst: `"Fly through pipes: +1\nGolden pipe: +2"`. Score tekst ima `raycastTarget = true`, pa tap na njega **ne** pokreće flap (TapCatcher je ispod).

#### 4.6.5 `SceneLoader` + tranzicija (R3)
- `SingletonPersistent`, sopstveni canvas (sorting order 1000) sa dva panela (gornja i donja polovina, Kenney boja + blaga senka/ivica na spoju).
- `Load(string sceneName)`: blokira input (`CanvasGroup.blocksRaycasts = true`), paneli klize ka sredini (0.35s, `InCubic`), Whoosh SFX, `SceneManager.LoadSceneAsync`, sačeka 1 frame posle load-a, `Time.timeScale = 1`, paneli se otvaraju (0.35s, `OutCubic`), input se odblokira.
- Ignoriše novi `Load` dok je tranzicija u toku. Tooltip i svi popupi se zatvaraju pre load-a.

#### 4.6.6 `PipeSpawnConfig` (ScriptableObject, R8 + težina)
Jedan asset `_ScriptableObjects/PipeSpawnConfig.asset`:
```csharp
[CreateAssetMenu(fileName = "PipeSpawnConfig", menuName = "ScriptableObjects/PipeSpawnConfig")]
public class PipeSpawnConfig : ScriptableObject
{
    [Header("Pipe Types")]
    public List<PipeTypeEntry> PipeTypes;   // Normal (weight 0.85, score 1), Bonus (weight 0.15, score 2)

    [Header("Spawning")]
    public float SpawnDistance = 3.4f;      // razmak između stubova u world jedinicama
    public float GapSize = 2.7f;
    public Vector2 GapCenterYRange = new(-1.6f, 2.0f);
    public float MaxGapCenterDelta = 2.2f;  // max skok centra između uzastopnih stubova (fer igra)
    public int PrewarmPerType = 4;

    [Header("Difficulty")]
    public float BaseSpeed = 2.4f;
    public float SpeedPerPoint = 0.05f;
    public float MaxSpeed = 4.0f;
}

[Serializable]
public class PipeTypeEntry
{
    public string Id;
    public PipePair Prefab;
    [Range(0f, 1f)] public float SpawnWeight;
    public int ScoreValue = 1;
}
```
- Težine su relativne (ne moraju da daju zbir 1). Izbor: `WeightedRandom.Pick` = isti cumulative algoritam kao `AllDecorationObjectsSO.GetSelectedDecoration` u Islands (preskače težinu ≤ 0, vraća `null` ako je total ≤ 0).
- `OnValidate` upozorava na prazan prefab ili negativnu težinu.
- Brzina = `Mathf.Min(BaseSpeed + SpeedPerPoint * score, MaxSpeed)`.
- Spawn je po **pređenoj distanci**, ne po vremenu → razmak između stubova ostaje isti i kad igra ubrzava.
- Sve vrednosti su **početne** — podešavaj ih dok igra ne bude prijatna.

#### 4.6.7 Object Pool (R7)
`ComponentPool<T> where T : Component` (`Core/ComponentPool.cs`), običan C#, bez singletona:
- `ComponentPool(T prefab, Transform parent, int prewarm)`
- `T Get()`: uzme iz `Stack<T>` ili `Instantiate`; `SetActive(true)`; ako implementira `IPoolable` → `OnSpawned()`.
- `void Release(T item)`: zaštita od duplog release-a (`HashSet<T>` aktivnih); `IPoolable.OnDespawned()`; `SetActive(false)`; push.
- `int CountActive`, `int CountInactive` (za verifikaciju).

`PipeSpawner` drži `Dictionary<PipeTypeEntry, ComponentPool<PipePair>>`. Svaki `PipePair` dobija `Action<PipePair> release` callback pri spawnu i sam sebe vraća u pool kad pređe `despawnX` (levo van kamere). Komentar u kodu objašnjava analogiju sa virtualizovanom scroll listom: konstantan mali broj objekata se reciklira i puni novim podacima (pozicija gapa, tip), umesto `Instantiate`/`Destroy` po stubu.

#### 4.6.8 Gameplay
- **`GameState`**: `Ready, Playing, Paused, Dead, GameOver`.
- **`GameManager`** (`Singleton`, Main scena) — state machine:
  - `Ready`: ptica lebdi (sine bob), prikazan "Get Ready" + tap hint. Prvi `OnFlapRequested` → `Playing` (i taj tap je prvi flap).
  - `Playing`: spawner i scroller rade. Prolaz kroz trigger stuba → `score += pipe.ScoreValue`, `RaiseScoreChanged(score, delta)`.
  - `OnPauseRequested` (samo iz `Playing`/`Ready`) → `Paused`, `Time.timeScale = 0`, `PopupManager.Show<PausePopup>(new PausePopupData(highScore))`.
  - `OnResumeRequested` → zatvori popup, `timeScale = 1`, vrati prethodno stanje.
  - `OnApplicationPause(true)` dok je `Playing` → automatski pauza (dobar mobilni UX).
  - `OnBirdDied` → `Dead`: stop spawner/scroller, input blokiran (BirdInput + Bird ignorišu flap van `Playing`/`Ready`), shake; korutina `WaitForSecondsRealtime(1f)` → ažuriraj high score (PlayerPrefs + `Save()`) → `GameOver` → `Show<GameOverPopup>(payload)`.
  - `OnRestartRequested` → `SceneLoader.Load("Main")`; `OnMenuRequested` → `timeScale = 1`, `SceneLoader.Load("Menu")`.
- **`Bird`**: `Rigidbody2D` (Dynamic, gravity scale ~2.2, interpolate, continuous collision), `CircleCollider2D`. Flap: `linearVelocity = new(0, 6f)`. Rotacija: +25° pri flapu, lerp ka -80° kad brzina padne ispod ~-3. Animacija krila: 3 frejma (ako izabrani sprajt ima frejmove) ili blagi squash na flap (DOTween `DOPunchScale`). Gornja granica: clamp Y (ne umire na plafonu). `OnCollisionEnter2D` sa stubom/zemljom → `RaiseBirdDied()` jednom; posle smrti pada do zemlje i ostaje.
- **`BirdInput`** (Gameplay): Input System akcija `Flap` mapirana **samo na tastaturu** (Space / W / UpArrow) → `RaiseFlapRequested()`.
- **`TapCatcher`** (UI): providna full-screen Image **iza** HUD elemenata, `IPointerDownHandler` → `RaiseFlapRequested()`. Pokriva i dodir (telefon) i levi klik miša (Editor). Pošto je ispod ostalog UI-ja, tap na Pause/score ne flapuje — rešava touch-vs-UI bez ručnih raycast provera, i nema duplog flapa jer miš nije mapiran u `BirdInput`.
- **`InputRouter`** (System): Esc / Android Back (Input System `Keyboard.escapeKey`) → ako `PopupManager.IsAnyOpen` → `HideTop()` (ili za Pause popup → `RaiseResumeRequested`), inače u Main sceni `RaisePauseRequested`, u Menu ne radi ništa.
- **`PipePair`**: gornji i donji stub (SpriteRenderer `drawMode = Tiled` za telo + cap sprajt), `Rigidbody2D` Kinematic koji se pomera `MovePosition` u `FixedUpdate`, `BoxCollider2D` po stubu, trigger `BoxCollider2D` u gapu za score (jednom po stubu). Bonus varijanta: zlatni sprajt + suptilan sjaj/pulse.
- **`LevelScroller`**: drži `CurrentSpeed` (iz configa i score-a), staje u `Dead`/`Paused`. `ScrollingLoop` za zemlju (brzina 1×) i pozadinu (parallax 0.3×), beskonačan loop sa dva segmenta.
- **`CameraShaker`**: na `OnBirdDied` → `DOShakePosition(0.35f, 0.25f, 20, 90)` sa vraćanjem na početnu poziciju.
- Kamera: orthographic, size 5, portrait. Zemlja ~1.2 jedinice od dna. Svet je dizajniran za 9:16; na širim/užim ekranima pozadina pokriva višak (bez crnih traka), a spawn/despawn X se računaju iz širine kamere.

#### 4.6.9 `AudioManager` (R1, R6)
- `SingletonPersistent`, `_sfxSource` + `_musicSource` (loop), `SoundData[]` lookup po `SoundId` enumu: `Click, Flap, Score, Bonus, Hit, Fall, Whoosh, PopupOpen`.
- Volumen: PlayerPrefs `SfxVolume` (default 0.8) i `MusicVolume` (default 0.5); učitava se u `Awake`, a čuva na promenu (`PlayerPrefs.Save()` na `OnApplicationPause` / hide Settings-a).
- Public getteri `SfxVolume` / `MusicVolume` za **controllere** (MenuController ih šalje u payload Settings popupa).
- Muzika kreće pri bootu i ne prekida se između scena; tokom pauze ide na 50%.
- `PlayOneShot` sa blagom random varijacijom pitch-a (0.95–1.05) za Flap/Click.

#### 4.6.10 `HapticsService` (R6)
- Android: `AndroidJavaClass("com.unity3d.player.UnityPlayer").currentActivity` → `getSystemService("vibrator")` → `VibrationEffect.createOneShot(ms, amplitude)` (API 26+). Light = 20ms / 60, Strong = 45ms / 180. Proveri `hasVibrator()`; Java objekte keširaj.
- Editor i ostale platforme: no-op (`Debug.Log` samo u verbose modu).
- **Dozvola VIBRATE**: Unity je dodaje u manifest samo kad u kodu postoji referenca na `Handheld.Vibrate`. Dodaj nepozvanu metodu `private static void EnsureVibratePermission() => Handheld.Vibrate();` sa komentarom zašto postoji. Proveri u APK-u (8.3).
- Sluša `OnButtonClicked` (Light) i `OnBirdDied` (Strong).

#### 4.6.11 Score UI (R9)
`ScoreView` sluša `OnScoreChanged`: postavi tekst, kill prethodnih tweenova, `DOPunchScale(Vector3.one * 0.35f, 0.25f, 8, 0.5f)`, boja → zelena `#4CD964` odmah, posle **0.2s** nazad na belu (`DOVirtual.DelayedCall(0.2f, …)` ili sekvenca). Bonus (+2) dobija i mali "+2" float tekst koji odleti i nestane.

### 4.7 UI layout i stil
- Canvas: Screen Space Overlay, `CanvasScaler` Scale With Screen Size, reference **1080×1920**, match 0.5. Root panel ima `SafeArea` komponentu (notch).
- Sorting order: HUD 0 → Popups 100 → Tooltip 200 → Transition 1000.
- Font: Kenney Future (iz UI Pack-a) → TMP font asset generisan builderom (`TMP_FontAsset.CreateFontAsset`); fallback LiberationSans SDF. Svi UI tekstovi na **engleskom** (Kenney font nema srpska slova č/ć/š/ž/đ).
- Tekstovi na svetloj pozadini imaju outline ili senku zbog čitljivosti.
- **Menu scena**: pozadina + zemlja koja skroluje (živ ekran), logo/naslov "FLAPPY" sa sporim bob-om, ptica lebdi, dugmad **Play** (veliko, blagi idle pulse kao `PlayButton` u Islands) i **Settings**, verzija igre u uglu.
- **Main scena HUD**: veliki score gore u sredini, Pause dugme (ikonica) gore desno, "Get Ready" + tap hint u `Ready` stanju (fade out na prvi tap).
- Minimalne dimenzije dodirnih površina: 120×120 ref px.

### 4.8 Persistencija (`Core/SaveKeys.cs`)
`HighScore` (int), `SfxVolume` (float), `MusicVolume` (float). Pristup PlayerPrefs-u imaju **samo** sistemi i controlleri, nikad UI.

### 4.9 Konvencije koda (iz Islands)
- `[SerializeField] private Type _camelCase;`, PascalCase za public članove i metode, expression-bodied za jednolinijske metode.
- Eventi `OnX`, handleri `HandleX`.
- `#region` samo gde stvarno pomaže; komentari retki i objašnjavaju **zašto**.
- Kešuj `GetComponent` u `Awake`; nema `Find*` u runtime kodu.
- Svaki DOTween tween na objektu koji može biti uništen ili reciklovan: `.SetLink(gameObject)`; UI tweenovi koji treba da rade tokom pauze: `.SetUpdate(true)`.

---

## 5. Art i audio

### 5.1 Kenney paketi (CC0)
| Paket | URL | Za šta |
|---|---|---|
| Tappy Plane | https://kenney.nl/assets/tappy-plane | pozadina, zemlja, "Get Ready"/tap hint sprajtovi |
| Animal Pack | https://kenney.nl/assets/animal-pack | ptica (okrugla ptica: chick/parrot/penguin — izaberi najčitljiviju; ako nijedna ne odgovara, avion iz Tappy Plane uz potvrdu korisnika) |
| UI Pack | https://kenney.nl/assets/ui-pack | dugmad (**normal + pressed** varijante za R5), paneli, slider, Kenney Future font |
| Game Icons | https://kenney.nl/assets/game-icons | ikonice pause, gear/settings, home, restart, play |
| Interface Sounds / UI Audio | https://kenney.nl/assets/interface-sounds , https://kenney.nl/assets/ui-audio | click, popup open, whoosh |
| Digital Audio | https://kenney.nl/assets/digital-audio | score (+1), bonus (+2), flap |
| Impact Sounds | https://kenney.nl/assets/impact-sounds | hit |

### 5.2 Download procedura
1. Download folder **van repoa**: `~/Desktop/UnityProjects/_kenney_downloads/` (svaki paket u svoj podfolder).
2. Zip URL se nalazi na stranici paketa (format `https://kenney.nl/media/pages/assets/<pack>/<hash>/kenney_<pack>.zip`) — izvuci ga iz HTML-a, ne pogađaj.
3. Raspakuj, pa u projekat kopiraj **samo fajlove koji se koriste** + `License.txt` u `Assets/_Art/Kenney/<pack>/` ili `Assets/_Audio/Kenney/<pack>/`. Ne kopiraj cele pakete.
4. Ako download ne uspe → stani i javi korisniku.
5. Dodaj `CREDITS.md` u root sa listom korišćenih paketa (CC0 ne zahteva atribuciju, ali je dobra praksa).

### 5.3 Generisani asseti (`GeneratedArtBuilder.cs`, Editor C#)
Kenney nema klasične Flappy stubove ni muzičku petlju, pa ih generiše Editor skripta (bez Pythona i spoljnih alata):
- **Stubovi**: `Texture2D` → `EncodeToPNG` → `Assets/_Art/Generated/`: `pipe_body_green.png` (tileable vertikalno, 128×64, flat stil sa svetlom trakom i tamnom ivicom), `pipe_cap_green.png` (160×64, zaobljeni uglovi), i ista dva u zlatnoj varijanti za Bonus. Paleta usklađena sa Kenney stilom.
- **Muzika**: `music_loop.wav` — C# piše PCM 16-bit mono 44.1kHz WAV (header + sample-ovi): vesela pentatonska melodija + bas, ~110 BPM, 8–16 taktova, besprekorno loop-ovanje (počinje i završava na nuli), peak oko -6 dBFS. Import: Load Type Streaming ili Compressed In Memory, Vorbis.
- Generator je deterministički (fiksni seed) i idempotentan.

### 5.4 Import podešavanja (`ArtImportBuilder.cs`)
- Sprajtovi: Texture Type Sprite, Pixels Per Unit konzistentan za svet (npr. 100) i odgovarajući za UI, Filter Bilinear, bez mipmapa, kompresija Normal. Pipe body: Mesh Type **Full Rect** (zbog Tiled draw mode), Wrap Mode Repeat.
- UI paneli/dugmad: postavi **9-slice border** u Sprite Editoru (preko `TextureImporter.spriteBorder`).
- SFX: Decompress On Load, Force Mono; muzika kao u 5.3.

---

## 6. Android

### 6.1 Player Settings (`ProjectSettingsBuilder.cs`, commit 1)
| Podešavanje | Vrednost |
|---|---|
| Company Name | `Mateja Milenkovic` |
| Product Name | `Flappy Bird` |
| Package Name | `com.matejamilenkovic.flappybird` |
| Version / Bundle Version Code | `0.1.0` / `1` |
| Default Orientation | **Portrait** (bez auto-rotacije) |
| Minimum API Level | **26** (Android 8.0 — potrebno za `VibrationEffect` bez fallback-a) |
| Target API Level | Automatic (highest installed) |
| Scripting Backend | IL2CPP |
| Target Architectures | ARM64 |
| Active Input Handling | Input System Package (New) — već postavljeno, samo proveri |
| Build scene list | `Assets/_Scenes/Menu.unity` (0), `Assets/_Scenes/Main.unity` (1) |
| Application.targetFrameRate | 60 (postavi u Bootstrap-u) |

### 6.2 Build (`AndroidBuilder.cs`)
- `public static void Build()` → `BuildPipeline.BuildPlayer` sa scenama iz Build Settings, target Android, output `Builds/Android/FlappyBird.apk` (`/Builds` i `*.apk` su već u `.gitignore`). Ne radi Development build. Potpisivanje: Unity debug keystore (za sideload je dovoljno).
- Pokretanje u živom Editoru: `unity command eval 'AndroidBuilder.Build();' --detach` + `unity job wait <id>` (IL2CPP build traje nekoliko minuta).
- **Pažnja**: `unity build` (batchmode) **ne može** da otvori projekat koji je već otvoren u Editoru. Ako ipak koristiš `unity build`, prvo `unity close <project>` (uz potvrdu korisnika), pa `unity build --target Android --execute-method AndroidBuilder.Build`. Tačne flagove proveri sa `unity build --help`.
- Build mora da prođe bez grešaka; zabeleži veličinu APK-a.

### 6.3 Provera APK-a
```bash
AAPT=/Applications/Unity/Hub/Editor/6000.3.9f1/PlaybackEngines/AndroidPlayer/SDK/build-tools/36.0.0/aapt
$AAPT dump badging Builds/Android/FlappyBird.apk | grep -E "package:|sdkVersion|targetSdkVersion|launchable-activity"
$AAPT dump permissions Builds/Android/FlappyBird.apk      # mora sadržati android.permission.VIBRATE
```

### 6.4 Test na uređaju
```bash
ADB=/Applications/Unity/Hub/Editor/6000.3.9f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb
$ADB devices
$ADB install -r Builds/Android/FlappyBird.apk
```
Ako je uređaj povezan (`adb devices` pokazuje `device`), agent sme da instalira i pokrene app (`adb shell monkey -p com.matejamilenkovic.flappybird 1`) i pročita `adb logcat -s Unity` za greške. Haptic, osećaj kontrola i izgled na ekranu potvrđuje **korisnik** — agent to ne može da proveri.

---

## 7. Git

- Pre svakog commita: `git status` — u commitu ne sme biti `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/`, `*.csproj`, `*.sln`. **Svaki asset mora imati svoj `.meta`**, i nijedan `.meta` ne sme ostati bez asseta.
- Dopuni `.gitignore`: `.DS_Store`, `.idea/`, `.vscode/` (ako već nisu tu).
- Provera iz 4.3 (grep za singletone u UI-ju) vraća prazno.
- Format poruke: `<oblast>: <kratak opis>` + telo sa listom stavki + `Co-Authored-By` linija ako je tvoj harness zahteva.
- Posle commita: `git push`. Ako push ne prođe, **ne** radi force push — javi korisniku.
- Ovaj dokument (`Implementation_definition.md`) ide u commit 1 ako još nije commitovan.

---

## 8. Plan commitova (5)

Svaki commit ostavlja projekat u stanju koje se kompajlira i pokreće. Redosled je bitan: svaki gradi na prethodnom.

### Commit 1 — `setup: project settings, assets, core systems`
**Obim**
- Provera preduslova (2.2). Instalacija `com.unity.pipeline` (3.1).
- `ProjectSettingsBuilder` (6.1), brisanje SampleScene, struktura foldera (4.2), dopuna `.gitignore`.
- Kenney download + import (5.1, 5.2), `ArtImportBuilder` (5.4), `GeneratedArtBuilder` (5.3), `CREDITS.md`.
- Core: `Singleton`, `GameEvents` (svi eventi iz 4.4, sa resetom), `SaveKeys`, `WeightedRandom`, `ComponentPool`, `Bootstrap`.
- Systems: `AudioManager`, `HapticsService`, `InputRouter` (bez scena za sada), `SystemsPrefabBuilder` → `Resources/Systems.prefab` (zasad sa AudioManager, HapticsService, InputRouter, EventSystem).
- Prazne `Menu.unity` i `Main.unity` u build listi.

**Verifikacija**
- `unity recompile` → 0 grešaka.
- Player Settings preko `eval` (`PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)`, orijentacija, min API) odgovaraju 6.1.
- Play u praznoj Main sceni → `Systems` instanciran, muzika svira (`AudioSource.isPlaying` preko `eval`), konzola bez grešaka.
- `eval`: `WeightedRandom.Pick` na 10 000 uzoraka sa težinama 0.85/0.15 → ~85/15 (±2%).
- `eval`: `ComponentPool` get/release/dupli release → ispravni brojači.

### Commit 2 — `gameplay: bird, pooled pipes with SO config, scoring, death`
**Obim**
- `PipeSpawnConfig` + asset (Normal 0.85 / Bonus 0.15), `PipePair` prefabi (Normal, Bonus), `PipeSpawner` sa `ComponentPool`, `LevelScroller`, `ScrollingLoop` (zemlja + parallax pozadina).
- `Bird`, `BirdInput` (Input System akcija Flap), `GameManager` (Ready/Playing/Dead/GameOver — bez popupa; privremeno: R restartuje scenu, samo u Editoru, uklanja se u commitu 4), `CameraShaker`.
- `GameplayPrefabBuilder`, `MainSceneBuilder` (kamera, svet, Bird, spawner; bez UI-ja).
- Zvukovi: Flap, Score, Bonus, Hit povezani preko evenata.

**Verifikacija**
- Play + `eval` simulacija: posle N sekundi bez input-a ptica pada i `OnBirdDied` se okine; sa periodičnim `GameEvents.RaiseFlapRequested()` score raste.
- `eval`: dok igra traje, `CountActive + CountInactive` po pool-u ostaje ograničen (nema rasta, nema `Instantiate` posle zagrevanja).
- Bonus stub daje +2 (forsiraj težine 0/1 privremeno preko `eval`, pa vrati).
- Brzina raste sa score-om i staje na `MaxSpeed`.
- Posle smrti `OnFlapRequested` ne menja brzinu ptice (input blokiran).
- Screenshot game view-a izgleda kao Flappy Bird (ptica, stubovi, zemlja, pozadina).

### Commit 3 — `ui framework: generic button, popup and tooltip systems, scene transition`
**Obim**
- `UIButton` + prefab + varijante; `PopupBase`, `PopupManager`; `TooltipManager`, `TooltipView`, `TooltipTrigger`; `SceneLoader` sa tranzicijom; `SafeArea`; TMP font asset.
- `UIPrefabBuilder`; ažuriraj `Systems.prefab` (PopupManager + canvas, TooltipManager + canvas, SceneLoader + transition canvas).
- Settings, Pause i GameOver popup prefabi (layout + skripte sa payload-om, sa ili bez wiring-a ka scenama).

**Verifikacija**
- `unity recompile` čisto; grep iz 4.3 prazan.
- U Play mode-u preko `eval`: `PopupManager.Instance.Show<SettingsPopup>(new SettingsPopupData(0.3f, 0.7f))` → slajderi pokazuju te vrednosti; `HideTop()` zatvara popup; screenshot oba stanja.
- `eval`: dupli `Show`/`Hide` tokom animacije ne baca izuzetak i završava u konzistentnom stanju.
- `eval`: tooltip prikazan uz element u uglu ekrana ostaje unutar safe area (flip/clamp radi).
- `eval`: `SceneLoader.Instance.Load("Main")` → tranzicija se odigra i scena se učita.

### Commit 4 — `screens & flow: main menu, settings, HUD, pause, game over, high score`
**Obim**
- `MenuSceneBuilder`: Menu scena (pozadina, naslov, ptica, Play + Settings), `MainMenuView`, `MenuController` (Play → `SceneLoader.Load("Main")`, Settings → `Show<SettingsPopup>` sa volumenima iz AudioManager-a).
- Main scena UI: `HudView` (score + pause), `ScoreView` (R9), `TapCatcher`, `GetReadyView`, `TooltipTrigger` na score-u.
- `GameManager`: pauza/resume/timeScale, auto-pauza, Game Over posle 1s sa payload-om, high score u PlayerPrefs, restart i menu preko SceneLoader-a. Ukloni privremeni R-restart.
- `InputRouter`: Back/Esc ponašanje iz 4.6.8.

**Verifikacija (kroz ceo tok)**
- Menu → Settings → pomeri slajdere (`eval` postavlja vrednost) → AudioManager volumen se menja → Play → tranzicija → Main → Ready → flap → score pop + zeleno 0.2s → Pause → popup pokazuje BEST → Resume → smrt → shake → posle ~1s GameOver sa tačnim score/best/NEW → Restart → Menu.
- High score preživljava izlazak iz Play mode-a i ponovni ulazak (PlayerPrefs).
- Volumeni preživljavaju restart.
- Tap na score otvara tooltip i **ne** flapuje; tap na Pause **ne** flapuje.
- Esc: zatvara popup / pauzira igru.
- Svaki klik dugmeta okida `OnButtonClicked` (brojač preko `eval`) → Click SFX.
- Grep iz 4.3 prazan. Screenshot svakog ekrana.

### Commit 5 — `polish & android: juice, audio mix, APK build`
**Obim**
- UX polish: Play idle pulse, naslov bob, "+2" float za bonus, NEW bedž, odbrojavanje score-a u GameOver-u, Fall zvuk, muzika 50% tokom pauze, provera da su sve animacije glatke na 60 FPS.
- Balans: podesi brzinu, gap i gravitaciju tako da su prvih ~10 stubova lagani.
- `AndroidBuilder` + build APK-a (6.2), provera APK-a (6.3), instalacija ako je uređaj povezan (6.4).
- Kratak `README.md`: kontrole, kako se builduje, struktura, gde je config.

**Verifikacija**
- APK se builduje bez grešaka; `aapt` pokazuje tačan package, minSdk 26 i `VIBRATE` dozvolu.
- (Ako je uređaj povezan) app se pokreće, `logcat` bez Unity izuzetaka.
- Finalna DoD lista (sekcija 9) prođena.

---

## 9. Definition of Done

- [ ] R1 Menu sa Play + Settings; Settings ima 2 odvojena slajdera koji stvarno menjaju SFX/muziku i čuvaju se.
- [ ] R2 Jedan `PopupBase` sa `Show()`/`Hide()` i DOTween animacijom, korišćen za Settings, Pause, GameOver.
- [ ] R3 Play → panel tranzicija → Main; ista tranzicija i za Restart/Menu.
- [ ] R4 HUD = score + pause; Pause popup: Resume, Menu, Best (PlayerPrefs, preživljava restart aplikacije).
- [ ] R5 Sva dugmad = `UIButton` skript + `UIButton` prefab (varijante), scale anim + sprite swap.
- [ ] R6 Svaki klik: click SFX + light haptic (VIBRATE u manifestu; osećaj potvrđuje korisnik na uređaju).
- [ ] R7 Stubovi idu kroz `ComponentPool`; nema `Instantiate`/`Destroy` posle zagrevanja.
- [ ] R8 Normal + Bonus sa težinama u jednom `PipeSpawnConfig` SO (i parametri težine).
- [ ] R9 Score pop + zeleno 0.2s na svako povećanje.
- [ ] R10 Smrt: shake, input blokiran, GameOver posle 1s.
- [ ] R11 Generički tooltip sistem (`TooltipTrigger` + `TooltipManager`), korišćen na score-u.
- [ ] R12 Grep iz 4.3 prazan; UI komunicira samo preko `GameEvents` + Popup/Tooltip singletona.
- [ ] R13 APK buildovan i proveren (`aapt`); igra radi tastaturom u Editoru i dodirom na telefonu.
- [ ] 5 commitova na `main`, svaki pushovan, svaki se kompajlira.

---

## 10. Poznati rizici

| Rizik | Šta uraditi |
|---|---|
| Pipeline paket je eksperimentalan (0.x-exp); `eval` se možda zove drugačije ili ne postoji | `unity command` lista je izvor istine; fallback iz 3.4 |
| Sandbox agenta skriva Editor od `unity status` | Ne zaključuj da je Editor zatvoren — pitaj korisnika |
| Compile greška → Editor u Safe Mode, CLI ne može da se poveže | `unity pipeline list`, popravi C# fajl, zamoli korisnika za restart Editora |
| Editor bez fokusa zamrznut u Play mode-u | Recept iz `playmode-verification-loop.md` |
| DOTween TMP modul nije uključen → `DOColor` na TMP ne postoji | Koristi `DOTween.To(() => text.color, c => text.color = c, …)` ili zamoli korisnika da uključi modul |
| `unity build` pada jer je projekat otvoren | Build kroz `eval` u živom Editoru (6.2) |
| Kenney fajl nije kakav je očekivan (npr. nema okrugle ptice) | Predloži alternativu korisniku pre nego što menjaš vizuelni smer |
| Statički eventi preživljavaju Play mode bez domain reload-a | Reset u `GameEvents` (4.4) + uvek unsubscribe u `OnDisable` |
