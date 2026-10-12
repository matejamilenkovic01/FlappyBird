using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MenuSceneBuilder
{
    private static readonly Vector3 BirdPosition = new(0f, 0.9f, 0f);
    private const float BirdScale = 1.4f;

    [MenuItem("Tools/Flappy/Builders/Menu Scene")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        MainSceneBuilder.CreateCamera();

        // Same world as the game, scrolling at a fixed speed so the menu feels alive.
        var world = new GameObject("World").transform;
        MainSceneBuilder.BuildBackground(world, null);
        MainSceneBuilder.BuildGround(world, null);

        SpriteRenderer bird = GameplayPrefabBuilder.CreateSprite(world, "MenuBird",
            GameplayPrefabBuilder.LoadSprite("Assets/_Art/Kenney/AnimalPack/parrot.png"), GameplayPrefabBuilder.BirdOrder, BirdPosition);
        bird.transform.localScale = Vector3.one * BirdScale;
        AddIdleMotion(bird.gameObject, 0, 0.18f, 0.8f);

        BuildUI();
        new GameObject("MenuController").AddComponent<MenuController>();

        EditorSceneManager.SaveScene(scene, ProjectSettingsBuilder.MenuScenePath);
        Debug.Log("MenuSceneBuilder: done");
    }

    private static void BuildUI()
    {
        var canvasObject = new GameObject("MenuUI");
        SystemsPrefabBuilder.CreateCanvas(canvasObject, 0, true);

        RectTransform safeArea = UIPrefabBuilder.CreateRect("SafeArea", canvasObject.transform);
        UIPrefabBuilder.Stretch(safeArea, Vector4.zero);
        safeArea.gameObject.AddComponent<SafeArea>();

        RectTransform title = UIPrefabBuilder.CreateRect("Title", safeArea);
        UIPrefabBuilder.SetTopAnchor(title);
        title.anchoredPosition = new Vector2(0, -360);
        title.sizeDelta = new Vector2(1000, 400);
        CreateTitleLine(title, "FLAPPY", 170, Color.white, 80);
        CreateTitleLine(title, "BIRD", 145, UIPrefabBuilder.Hex("FFD34D"), -100);
        AddIdleMotion(title.gameObject, 0, 18f, 1.2f);

        // The pulse lives on a wrapper so it never fights the button's own press scale tween.
        RectTransform playPulse = UIPrefabBuilder.CreateRect("PlayPulse", safeArea);
        playPulse.anchoredPosition = new Vector2(0, -250);
        AddIdleMotion(playPulse.gameObject, 1, 1.06f, 0.6f);
        UIButton play = UIPrefabBuilder.InstantiateButton(UIPrefabBuilder.ButtonVariant.Play, playPulse, Vector2.zero);
        UIButton settings = UIPrefabBuilder.InstantiateButton(UIPrefabBuilder.ButtonVariant.Settings, safeArea, new Vector2(0, -470));

        TMP_Text version = UIPrefabBuilder.CreateText(safeArea, "Version", "v0.0.0", 40, new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.BottomRight);
        version.fontSharedMaterial = UIPrefabBuilder.OutlineMaterial;
        version.rectTransform.anchorMin = version.rectTransform.anchorMax = new Vector2(1f, 0f);
        version.rectTransform.pivot = new Vector2(1f, 0f);
        version.rectTransform.anchoredPosition = new Vector2(-32, 28);
        version.rectTransform.sizeDelta = new Vector2(300, 60);

        var view = canvasObject.AddComponent<MainMenuView>();
        MainSceneBuilder.SetField(view, "_playButton", play);
        MainSceneBuilder.SetField(view, "_settingsButton", settings);
        MainSceneBuilder.SetField(view, "_versionText", version);
    }

    // mode: 0 = bob, 1 = pulse (IdleMotion.Mode)
    private static void AddIdleMotion(GameObject go, int mode, float amount, float halfPeriod)
    {
        var motion = go.AddComponent<IdleMotion>();
        var serialized = new SerializedObject(motion);
        serialized.FindProperty("_mode").enumValueIndex = mode;
        serialized.FindProperty("_amount").floatValue = amount;
        serialized.FindProperty("_halfPeriod").floatValue = halfPeriod;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateTitleLine(RectTransform parent, string text, float size, Color color, float y)
    {
        TMP_Text line = UIPrefabBuilder.CreateText(parent, text, text, size, color, TextAlignmentOptions.Center);
        line.fontSharedMaterial = UIPrefabBuilder.OutlineMaterial;
        line.rectTransform.anchoredPosition = new Vector2(0, y);
        line.rectTransform.sizeDelta = new Vector2(1000, size * 1.2f);
    }
}
