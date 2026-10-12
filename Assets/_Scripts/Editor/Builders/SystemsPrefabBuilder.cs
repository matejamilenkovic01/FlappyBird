using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class SystemsPrefabBuilder
{
    public const string PrefabPath = "Assets/Resources/Systems.prefab";

    private const string AudioRoot = "Assets/_Audio/Kenney";

    private static readonly (SoundId id, string path, float volume)[] Sounds =
    {
        (SoundId.Click, AudioRoot + "/UIAudio/click1.ogg", 0.8f),
        (SoundId.Flap, AudioRoot + "/DigitalAudio/phaseJump1.ogg", 0.45f),
        (SoundId.Score, AudioRoot + "/DigitalAudio/pepSound1.ogg", 0.6f),
        (SoundId.Bonus, AudioRoot + "/DigitalAudio/powerUp2.ogg", 0.7f),
        (SoundId.Hit, AudioRoot + "/ImpactSounds/impactPunch_heavy_000.ogg", 1f),
        (SoundId.Fall, AudioRoot + "/DigitalAudio/phaserDown1.ogg", 0.6f),
        (SoundId.Whoosh, AudioRoot + "/InterfaceSounds/minimize_006.ogg", 0.7f),
        (SoundId.PopupOpen, AudioRoot + "/InterfaceSounds/maximize_006.ogg", 0.6f)
    };

    [MenuItem("Tools/Flappy/Builders/Systems Prefab")]
    public static void Build()
    {
        using var scope = new TempObjectScope();
        var root = new GameObject("Systems");

        BuildAudio(root.transform);
        CreateChild<HapticsService>(root.transform, "HapticsService");
        CreateChild<InputRouter>(root.transform, "InputRouter");
        BuildEventSystem(root.transform);
        BuildPopupManager(root.transform);
        BuildTooltipManager(root.transform);
        BuildSceneLoader(root.transform);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log("SystemsPrefabBuilder: done");
    }

    private static void BuildAudio(Transform parent)
    {
        AudioManager audioManager = CreateChild<AudioManager>(parent, "AudioManager");
        GameObject go = audioManager.gameObject;
        // The only listener in the game: it persists across scenes, so scene cameras don't carry one.
        go.AddComponent<AudioListener>();

        AudioSource sfxSource = go.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        AudioSource musicSource = go.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;

        var serialized = new SerializedObject(audioManager);
        serialized.FindProperty("_sfxSource").objectReferenceValue = sfxSource;
        serialized.FindProperty("_musicSource").objectReferenceValue = musicSource;
        serialized.FindProperty("_musicClip").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Audio/Generated/music_loop.wav");

        SerializedProperty sounds = serialized.FindProperty("_sounds");
        sounds.arraySize = Sounds.Length;
        for (int i = 0; i < Sounds.Length; i++)
        {
            SerializedProperty element = sounds.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("Id").enumValueIndex = (int)Sounds[i].id;
            element.FindPropertyRelative("Clip").objectReferenceValue = LoadRequired<AudioClip>(Sounds[i].path);
            element.FindPropertyRelative("Volume").floatValue = Sounds[i].volume;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildEventSystem(Transform parent)
    {
        var go = new GameObject("EventSystem");
        go.transform.SetParent(parent, false);
        go.AddComponent<EventSystem>();
        // Actions left unassigned on purpose: the module falls back to the Input System's default UI
        // actions at runtime. Assigning them here would only store in-memory references that don't serialize.
        go.AddComponent<InputSystemUIInputModule>();
    }

    private static void BuildPopupManager(Transform parent)
    {
        PopupManager manager = CreateChild<PopupManager>(parent, "PopupManager");
        CreateCanvas(manager.gameObject, 100, true);

        var serialized = new SerializedObject(manager);
        serialized.FindProperty("_container").objectReferenceValue = manager.transform;
        SerializedProperty prefabs = serialized.FindProperty("_popupPrefabs");
        string[] paths = { UIPrefabBuilder.SettingsPopupPath, UIPrefabBuilder.PausePopupPath, UIPrefabBuilder.GameOverPopupPath };
        prefabs.arraySize = paths.Length;
        for (int i = 0; i < paths.Length; i++)
            prefabs.GetArrayElementAtIndex(i).objectReferenceValue = LoadRequired<GameObject>(paths[i]).GetComponent<PopupBase>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildTooltipManager(Transform parent)
    {
        TooltipManager manager = CreateChild<TooltipManager>(parent, "TooltipManager");
        // No raycaster: the tooltip never blocks taps meant for the game.
        CreateCanvas(manager.gameObject, 200, false);

        var serialized = new SerializedObject(manager);
        serialized.FindProperty("_canvasRect").objectReferenceValue = manager.transform;
        serialized.FindProperty("_viewPrefab").objectReferenceValue =
            LoadRequired<GameObject>(UIPrefabBuilder.TooltipViewPath).GetComponent<TooltipView>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildSceneLoader(Transform parent)
    {
        SceneLoader loader = CreateChild<SceneLoader>(parent, "SceneLoader");
        CreateCanvas(loader.gameObject, 1000, true);
        var blocker = loader.gameObject.AddComponent<CanvasGroup>();
        blocker.blocksRaycasts = false;

        RectTransform top = BuildTransitionPanel(loader.transform, "TopPanel", true);
        RectTransform bottom = BuildTransitionPanel(loader.transform, "BottomPanel", false);

        var serialized = new SerializedObject(loader);
        serialized.FindProperty("_blocker").objectReferenceValue = blocker;
        serialized.FindProperty("_topPanel").objectReferenceValue = top;
        serialized.FindProperty("_bottomPanel").objectReferenceValue = bottom;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // Each panel covers half the screen; the seam gets a darker edge and a soft shadow onto the other half.
    private static RectTransform BuildTransitionPanel(Transform parent, string name, bool isTop)
    {
        Color panelColor = UIPrefabBuilder.Hex("4A9BD9");
        Color edgeColor = UIPrefabBuilder.Hex("2F74AE");
        const float overlap = 6f;
        const float edgeHeight = 16f;
        const float shadowHeight = 26f;

        RectTransform panel = UIPrefabBuilder.CreateRect(name, parent);
        // Start fully open (off screen); SceneLoader drives the anchors from here.
        panel.anchorMin = isTop ? new Vector2(0f, 1.04f) : new Vector2(0f, -0.54f);
        panel.anchorMax = isTop ? new Vector2(1f, 1.54f) : new Vector2(1f, -0.04f);
        panel.offsetMin = new Vector2(0f, isTop ? -overlap : 0f);
        panel.offsetMax = new Vector2(0f, isTop ? 0f : overlap);
        Image image = UIPrefabBuilder.AddImage(panel.gameObject, null, panelColor, false);
        image.raycastTarget = true;

        float seamY = isTop ? 0f : 1f;
        RectTransform shadow = UIPrefabBuilder.CreateRect("Shadow", panel);
        shadow.anchorMin = new Vector2(0f, seamY);
        shadow.anchorMax = new Vector2(1f, seamY);
        shadow.pivot = new Vector2(0.5f, isTop ? 1f : 0f);
        shadow.sizeDelta = new Vector2(0f, shadowHeight);
        UIPrefabBuilder.AddImage(shadow.gameObject, null, new Color(0f, 0f, 0f, 0.18f), false);

        RectTransform edge = UIPrefabBuilder.CreateRect("Edge", panel);
        edge.anchorMin = new Vector2(0f, seamY);
        edge.anchorMax = new Vector2(1f, seamY);
        edge.pivot = new Vector2(0.5f, isTop ? 0f : 1f);
        edge.sizeDelta = new Vector2(0f, edgeHeight);
        UIPrefabBuilder.AddImage(edge.gameObject, null, edgeColor, false);

        return panel;
    }

    public static Canvas CreateCanvas(GameObject go, int sortingOrder, bool raycaster)
    {
        go.layer = LayerMask.NameToLayer("UI");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (raycaster)
            go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static T CreateChild<T>(Transform parent, string name) where T : Component
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<T>();
    }

    private static T LoadRequired<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            Debug.LogError($"SystemsPrefabBuilder: missing asset {path}");
        return asset;
    }
}
