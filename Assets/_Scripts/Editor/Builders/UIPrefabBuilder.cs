using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class UIPrefabBuilder
{
    public const string UIFolder = "Assets/_Prefabs/UI";
    public const string PopupFolder = "Assets/_Prefabs/Popups";
    public const string FontFolder = "Assets/_Art/Fonts";
    public const string FontAssetPath = FontFolder + "/KenneyFuture SDF.asset";
    public const string OutlineMaterialPath = FontFolder + "/KenneyFuture SDF Outline.mat";

    public const string ButtonPath = UIFolder + "/UIButton.prefab";
    public const string SliderPath = UIFolder + "/UISlider.prefab";
    public const string TooltipViewPath = UIFolder + "/TooltipView.prefab";
    public const string SettingsPopupPath = PopupFolder + "/SettingsPopup.prefab";
    public const string PausePopupPath = PopupFolder + "/PausePopup.prefab";
    public const string GameOverPopupPath = PopupFolder + "/GameOverPopup.prefab";

    public static readonly Color TextBrown = Hex("7A4A12");
    public static readonly Color TextBrownLight = Hex("A57A44");
    public static readonly Color OutlineColor = Hex("3A2A14");
    private static readonly Color TooltipColor = Hex("2E3B4E");

    private const string Art = "Assets/_Art/Kenney";
    private const string Icons = Art + "/GameIcons";

    public enum ButtonVariant { Play, Settings, Resume, Menu, Restart, Close, Pause }

    private static readonly Dictionary<ButtonVariant, (string label, string icon, Vector2 size, float fontSize)> Variants = new()
    {
        { ButtonVariant.Play, ("PLAY", "icon_right", new Vector2(580, 180), 80) },
        { ButtonVariant.Settings, ("SETTINGS", "icon_gear", new Vector2(580, 150), 60) },
        { ButtonVariant.Resume, ("RESUME", "icon_right", new Vector2(520, 150), 60) },
        { ButtonVariant.Menu, ("MENU", "icon_home", new Vector2(380, 150), 60) },
        { ButtonVariant.Restart, ("RETRY", "icon_return", new Vector2(380, 150), 60) },
        { ButtonVariant.Close, ("CLOSE", null, new Vector2(420, 150), 60) },
        { ButtonVariant.Pause, (null, "icon_pause", new Vector2(150, 150), 60) }
    };

    [MenuItem("Tools/Flappy/Builders/UI Prefabs")]
    public static void BuildAll()
    {
        using var scope = new TempObjectScope();
        GameplayPrefabBuilder.EnsureFolder("Assets/_Prefabs", "UI");
        GameplayPrefabBuilder.EnsureFolder("Assets/_Prefabs", "Popups");
        GameplayPrefabBuilder.EnsureFolder("Assets/_Art", "Fonts");

        EnsureFont();
        BuildButton();
        foreach (ButtonVariant variant in Variants.Keys)
            BuildButtonVariant(variant);
        BuildSlider();
        BuildTooltipView();
        BuildSettingsPopup();
        BuildPausePopup();
        BuildGameOverPopup();

        AssetDatabase.SaveAssets();
        Debug.Log("UIPrefabBuilder: done");
    }

    public static string VariantPath(ButtonVariant variant) => $"{UIFolder}/UIButton_{variant}.prefab";

    #region Font
    public static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
    public static Material OutlineMaterial => AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);

    // Created once and kept, so its GUID (referenced by every text) stays stable across rebuilds.
    private static void EnsureFont()
    {
        TMP_FontAsset fontAsset = Font;
        if (fontAsset == null)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(Art + "/UIPack/KenneyFuture.ttf");
            fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            fontAsset.name = "KenneyFuture SDF";

            var ascii = new System.Text.StringBuilder();
            for (char c = ' '; c <= '~'; c++)
                ascii.Append(c);
            fontAsset.TryAddCharacters(ascii.ToString());
            // Static atlas: playing the game never rewrites the asset (no git noise).
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            fontAsset.material.name = "KenneyFuture SDF Material";
            fontAsset.atlasTexture.name = "KenneyFuture SDF Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        }

        fontAsset.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
        if (TMP_Settings.defaultFontAsset != null && !fontAsset.fallbackFontAssetTable.Contains(TMP_Settings.defaultFontAsset))
            fontAsset.fallbackFontAssetTable.Add(TMP_Settings.defaultFontAsset);
        EditorUtility.SetDirty(fontAsset);

        Material outline = OutlineMaterial;
        if (outline == null)
        {
            outline = new Material(fontAsset.material) { name = "KenneyFuture SDF Outline" };
            AssetDatabase.CreateAsset(outline, OutlineMaterialPath);
        }
        outline.shader = fontAsset.material.shader;
        outline.SetTexture(ShaderUtilities.ID_MainTex, fontAsset.atlasTexture);
        outline.EnableKeyword(ShaderUtilities.Keyword_Outline);
        outline.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
        outline.SetColor(ShaderUtilities.ID_OutlineColor, OutlineColor);
        outline.SetFloat(ShaderUtilities.ID_FaceDilate, 0.15f);
        EditorUtility.SetDirty(outline);
    }
    #endregion

    #region Button
    private static void BuildButton()
    {
        RectTransform root = CreateRect("UIButton", null);
        root.sizeDelta = new Vector2(480, 150);
        Image image = AddImage(root.gameObject, LoadSprite(Art + "/UIPack/button_yellow_normal.png"), Color.white, true);
        image.raycastTarget = true;

        RectTransform content = CreateRect("Content", root);
        Stretch(content, new Vector4(36, 26, 36, 12));
        var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 26f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        RectTransform icon = CreateRect("Icon", content);
        AddImage(icon.gameObject, null, TextBrown, false);
        var iconLayout = icon.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 76f;
        iconLayout.preferredHeight = 76f;
        icon.gameObject.SetActive(false);

        TMP_Text label = CreateText(content, "Label", "BUTTON", 60, TextBrown, TextAlignmentOptions.Center);
        label.textWrappingMode = TextWrappingModes.NoWrap;

        UIButton button = root.gameObject.AddComponent<UIButton>();
        var serialized = new SerializedObject(button);
        serialized.FindProperty("_normalSprite").objectReferenceValue = image.sprite;
        serialized.FindProperty("_pressedSprite").objectReferenceValue = LoadSprite(Art + "/UIPack/button_yellow_pressed.png");
        serialized.FindProperty("_content").objectReferenceValue = content;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root.gameObject, ButtonPath);
    }

    private static void BuildButtonVariant(ButtonVariant variant)
    {
        var (label, icon, size, fontSize) = Variants[variant];
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        instance.name = $"UIButton_{variant}";
        ((RectTransform)instance.transform).sizeDelta = size;

        Transform content = instance.transform.Find("Content");
        var labelText = content.Find("Label").GetComponent<TMP_Text>();
        labelText.gameObject.SetActive(label != null);
        if (label != null)
        {
            labelText.text = label;
            labelText.fontSize = fontSize;
        }

        var iconImage = content.Find("Icon").GetComponent<Image>();
        iconImage.gameObject.SetActive(icon != null);
        if (icon != null)
        {
            iconImage.sprite = LoadSprite($"{Icons}/{icon}.png");
            float iconSize = label == null ? 84f : fontSize * 1.25f;
            var iconLayout = iconImage.GetComponent<LayoutElement>();
            iconLayout.preferredWidth = iconSize;
            iconLayout.preferredHeight = iconSize;
        }

        PrefabUtility.SaveAsPrefabAsset(instance, VariantPath(variant));
        Object.DestroyImmediate(instance);
    }

    public static UIButton InstantiateButton(ButtonVariant variant, Transform parent, Vector2 anchoredPosition)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath(variant));
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        var rect = (RectTransform)instance.transform;
        rect.anchoredPosition = anchoredPosition;
        return instance.GetComponent<UIButton>();
    }
    #endregion

    #region Slider
    private static void BuildSlider()
    {
        RectTransform root = CreateRect("UISlider", null);
        root.sizeDelta = new Vector2(640, 80);

        RectTransform background = CreateRect("Background", root);
        background.anchorMin = new Vector2(0f, 0.5f);
        background.anchorMax = new Vector2(1f, 0.5f);
        background.sizeDelta = new Vector2(0f, 36f);
        AddImage(background.gameObject, LoadSprite(Art + "/UIPack/slider_track.png"), Color.white, true);

        RectTransform fillArea = CreateRect("Fill Area", root);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.sizeDelta = new Vector2(0f, 36f);
        RectTransform fill = CreateRect("Fill", fillArea);
        Stretch(fill, Vector4.zero);
        AddImage(fill.gameObject, LoadSprite(Art + "/UIPack/slider_fill.png"), Color.white, true);

        RectTransform handleArea = CreateRect("Handle Slide Area", root);
        Stretch(handleArea, new Vector4(28, 0, 28, 0));
        RectTransform handle = CreateRect("Handle", handleArea);
        handle.sizeDelta = new Vector2(60f, 80f);
        Image handleImage = AddImage(handle.gameObject, LoadSprite(Art + "/UIPack/slider_handle.png"), Color.white, false);
        handleImage.raycastTarget = true;

        var slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        var colors = slider.colors;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
        slider.colors = colors;

        // The whole bar is draggable, not only the handle.
        background.GetComponent<Image>().raycastTarget = true;

        SavePrefab(root.gameObject, SliderPath);
    }

    private static Slider InstantiateSlider(Transform parent, Vector2 anchoredPosition)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SliderPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        ((RectTransform)instance.transform).anchoredPosition = anchoredPosition;
        return instance.GetComponent<Slider>();
    }
    #endregion

    #region Tooltip
    private static void BuildTooltipView()
    {
        RectTransform root = CreateRect("TooltipView", null);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 1f);
        root.sizeDelta = new Vector2(500, 160);
        AddImage(root.gameObject, LoadSprite(Art + "/UIPack/tooltip_bg.png"), TooltipColor, true);
        root.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

        TMP_Text text = CreateText(root, "Text", "Tooltip", 38, Color.white, TextAlignmentOptions.Center);
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.lineSpacing = 10f;

        RectTransform arrow = CreateRect("Arrow", root);
        arrow.sizeDelta = new Vector2(48, 24);
        AddImage(arrow.gameObject, LoadSprite("Assets/_Art/Generated/tooltip_arrow.png"), TooltipColor, false);

        TooltipView view = root.gameObject.AddComponent<TooltipView>();
        var serialized = new SerializedObject(view);
        serialized.FindProperty("_text").objectReferenceValue = text;
        serialized.FindProperty("_arrow").objectReferenceValue = arrow;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root.gameObject, TooltipViewPath);
    }
    #endregion

    #region Popups
    private static void BuildSettingsPopup()
    {
        RectTransform panel = CreatePopupShell("SettingsPopup", new Vector2(900, 1000), "SETTINGS", out GameObject root);

        CreateIconLabel(panel, "icon_audioOn", "SOUND", new Vector2(0, -250));
        Slider sfx = InstantiateSlider(panel, new Vector2(0, -350));
        SetTopAnchor((RectTransform)sfx.transform);

        CreateIconLabel(panel, "icon_musicOn", "MUSIC", new Vector2(0, -490));
        Slider music = InstantiateSlider(panel, new Vector2(0, -590));
        SetTopAnchor((RectTransform)music.transform);

        UIButton close = InstantiateButton(ButtonVariant.Close, panel, new Vector2(0, 130));
        SetBottomAnchor((RectTransform)close.transform);

        var popup = root.AddComponent<SettingsPopup>();
        WirePopup(popup, closeOnBack: true, closeOnDimClick: true);
        var serialized = new SerializedObject(popup);
        serialized.FindProperty("_sfxSlider").objectReferenceValue = sfx;
        serialized.FindProperty("_musicSlider").objectReferenceValue = music;
        serialized.FindProperty("_closeButton").objectReferenceValue = close;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, SettingsPopupPath);
    }

    private static void BuildPausePopup()
    {
        RectTransform panel = CreatePopupShell("PausePopup", new Vector2(900, 820), "PAUSED", out GameObject root);

        TMP_Text best = CreateText(panel, "Best", "BEST: 0", 64, TextBrownLight, TextAlignmentOptions.Center);
        SetTopAnchor(best.rectTransform);
        best.rectTransform.anchoredPosition = new Vector2(0, -235);
        best.rectTransform.sizeDelta = new Vector2(800, 90);

        UIButton resume = InstantiateButton(ButtonVariant.Resume, panel, new Vector2(0, -400));
        SetTopAnchor((RectTransform)resume.transform);
        UIButton menu = InstantiateButton(ButtonVariant.Menu, panel, new Vector2(0, -590));
        SetTopAnchor((RectTransform)menu.transform);

        var popup = root.AddComponent<PausePopup>();
        WirePopup(popup, closeOnBack: true, closeOnDimClick: false);
        var serialized = new SerializedObject(popup);
        serialized.FindProperty("_bestText").objectReferenceValue = best;
        serialized.FindProperty("_resumeButton").objectReferenceValue = resume;
        serialized.FindProperty("_menuButton").objectReferenceValue = menu;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, PausePopupPath);
    }

    private static void BuildGameOverPopup()
    {
        RectTransform panel = CreatePopupShell("GameOverPopup", new Vector2(900, 940), "GAME OVER", out GameObject root);

        CreateTopText(panel, "ScoreLabel", "SCORE", 48, TextBrownLight, -235);
        TMP_Text score = CreateTopText(panel, "Score", "0", 120, Color.white, -340);
        score.fontSharedMaterial = OutlineMaterial;
        CreateTopText(panel, "BestLabel", "BEST", 48, TextBrownLight, -480);
        TMP_Text best = CreateTopText(panel, "Best", "0", 84, TextBrown, -570);

        RectTransform badge = CreateRect("NewBadge", panel);
        SetTopAnchor(badge);
        badge.anchoredPosition = new Vector2(210, -470);
        badge.sizeDelta = new Vector2(170, 72);
        badge.localRotation = Quaternion.Euler(0, 0, 12f);
        AddImage(badge.gameObject, LoadSprite(Art + "/UIPack/badge_red.png"), Color.white, true);
        TMP_Text badgeText = CreateText(badge, "Text", "NEW", 44, Color.white, TextAlignmentOptions.Center);
        Stretch(badgeText.rectTransform, new Vector4(0, 6, 0, 0));
        badge.gameObject.SetActive(false);

        UIButton restart = InstantiateButton(ButtonVariant.Restart, panel, new Vector2(-205, 140));
        SetBottomAnchor((RectTransform)restart.transform);
        UIButton menu = InstantiateButton(ButtonVariant.Menu, panel, new Vector2(205, 140));
        SetBottomAnchor((RectTransform)menu.transform);

        var popup = root.AddComponent<GameOverPopup>();
        WirePopup(popup, closeOnBack: false, closeOnDimClick: false);
        var serialized = new SerializedObject(popup);
        serialized.FindProperty("_scoreText").objectReferenceValue = score;
        serialized.FindProperty("_bestText").objectReferenceValue = best;
        serialized.FindProperty("_newBadge").objectReferenceValue = badge;
        serialized.FindProperty("_restartButton").objectReferenceValue = restart;
        serialized.FindProperty("_menuButton").objectReferenceValue = menu;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, GameOverPopupPath);
    }

    private static RectTransform CreatePopupShell(string name, Vector2 panelSize, string title, out GameObject root)
    {
        RectTransform rootRect = CreateRect(name, null);
        Stretch(rootRect, Vector4.zero);
        rootRect.gameObject.AddComponent<CanvasGroup>();

        RectTransform dim = CreateRect("Dim", rootRect);
        Stretch(dim, Vector4.zero);
        Image dimImage = AddImage(dim.gameObject, null, new Color(0f, 0f, 0f, 0.6f), false);
        dimImage.raycastTarget = true;
        dim.gameObject.AddComponent<CanvasGroup>();

        RectTransform panel = CreateRect("Panel", rootRect);
        panel.sizeDelta = panelSize;
        Image panelImage = AddImage(panel.gameObject, LoadSprite(Art + "/TappyPlane/UIbg.png"), Color.white, true);
        panelImage.raycastTarget = true;
        // The source panel is small; a lower multiplier keeps its rounded corners in proportion.
        panelImage.pixelsPerUnitMultiplier = 0.5f;
        panel.gameObject.AddComponent<CanvasGroup>();

        TMP_Text titleText = CreateTopText(panel, "Title", title, 88, Color.white, -110);
        titleText.fontSharedMaterial = OutlineMaterial;

        root = rootRect.gameObject;
        return panel;
    }

    private static void WirePopup(PopupBase popup, bool closeOnBack, bool closeOnDimClick)
    {
        var serialized = new SerializedObject(popup);
        serialized.FindProperty("_dim").objectReferenceValue = popup.transform.Find("Dim").GetComponent<CanvasGroup>();
        serialized.FindProperty("_panel").objectReferenceValue = popup.transform.Find("Panel");
        serialized.FindProperty("_closeOnBack").boolValue = closeOnBack;
        serialized.FindProperty("_closeOnDimClick").boolValue = closeOnDimClick;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateIconLabel(RectTransform parent, string icon, string label, Vector2 position)
    {
        RectTransform row = CreateRect(label + "Row", parent);
        SetTopAnchor(row);
        row.anchoredPosition = position;
        row.sizeDelta = new Vector2(640, 90);
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = 20f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        RectTransform iconRect = CreateRect("Icon", row);
        AddImage(iconRect.gameObject, LoadSprite($"{Icons}/{icon}.png"), TextBrown, false);
        var iconLayout = iconRect.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 70f;
        iconLayout.preferredHeight = 70f;

        CreateText(row, "Label", label, 56, TextBrown, TextAlignmentOptions.Left);
    }

    private static TMP_Text CreateTopText(RectTransform parent, string name, string text, float size, Color color, float y)
    {
        TMP_Text tmp = CreateText(parent, name, text, size, color, TextAlignmentOptions.Center);
        SetTopAnchor(tmp.rectTransform);
        tmp.rectTransform.anchoredPosition = new Vector2(0, y);
        tmp.rectTransform.sizeDelta = new Vector2(820, size * 1.3f);
        return tmp;
    }
    #endregion

    #region Helpers
    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        if (parent != null)
            go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    // padding: left, bottom, right, top
    public static void Stretch(RectTransform rect, Vector4 padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding.x, padding.y);
        rect.offsetMax = new Vector2(-padding.z, -padding.w);
    }

    public static void SetTopAnchor(RectTransform rect)
    {
        Vector2 position = rect.anchoredPosition;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
    }

    public static void SetBottomAnchor(RectTransform rect)
    {
        Vector2 position = rect.anchoredPosition;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
    }

    public static Image AddImage(GameObject go, Sprite sprite, Color color, bool sliced)
    {
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = !sliced && sprite != null;
        image.raycastTarget = false;
        return image;
    }

    public static TMP_Text CreateText(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = Font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    public static Sprite LoadSprite(string path) => GameplayPrefabBuilder.LoadSprite(path);

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color color);
        return color;
    }
    #endregion
}
