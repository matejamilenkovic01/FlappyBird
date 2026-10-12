using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ArtImportBuilder
{
    public const string KenneyArt = "Assets/_Art/Kenney";
    public const string GeneratedArt = "Assets/_Art/Generated";
    public const string KenneyAudio = "Assets/_Audio/Kenney";
    public const string GeneratedAudio = "Assets/_Audio/Generated";

    private const float WorldPixelsPerUnit = 100f;
    private const float UIPixelsPerUnit = 100f;

    // Per-sprite overrides that size world sprites to the 10-unit-tall portrait view.
    private static readonly Dictionary<string, float> PixelsPerUnitOverrides = new()
    {
        { "AnimalPack/parrot.png", 450f },      // 284px -> ~0.63 units
        { "TappyPlane/background.png", 48f },    // 480px -> 10 units, covers the full view height
        { "pipe_", 128f }                        // 128px body -> 1 unit wide
    };

    // 9-slice borders (left, bottom, right, top) in source pixels.
    private static readonly Dictionary<string, Vector4> Borders = new()
    {
        { "UIPack/button_yellow_normal.png", new Vector4(20, 26, 20, 20) },
        { "UIPack/button_yellow_pressed.png", new Vector4(20, 20, 20, 20) },
        { "UIPack/slider_track.png", new Vector4(14, 14, 14, 14) },
        { "UIPack/slider_fill.png", new Vector4(14, 14, 14, 14) },
        { "TappyPlane/UIbg.png", new Vector4(16, 16, 16, 16) },
        { "ui_white.png", new Vector4(1, 1, 1, 1) }
    };

    private static readonly string[] WorldSpriteKeys =
    {
        "AnimalPack/", "TappyPlane/background.png", "TappyPlane/groundGrass.png", "pipe_", "world_white.png"
    };

    [MenuItem("Tools/Flappy/Builders/Art Import")]
    public static void Apply()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { KenneyArt, GeneratedArt }))
            ApplySprite(AssetDatabase.GUIDToAssetPath(guid));

        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { KenneyAudio }))
            ApplySfx(AssetDatabase.GUIDToAssetPath(guid));

        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { GeneratedAudio }))
            ApplyMusic(AssetDatabase.GUIDToAssetPath(guid));

        Debug.Log("ArtImportBuilder: done");
    }

    private static void ApplySprite(string path)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            return;

        bool isWorld = MatchesAny(path, WorldSpriteKeys);
        bool isPipeBody = path.Contains("pipe_body");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = FindValue(path, PixelsPerUnitOverrides, isWorld ? WorldPixelsPerUnit : UIPixelsPerUnit);
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.alphaIsTransparency = true;
        importer.wrapMode = isPipeBody ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        // Tiled draw mode needs a full-rect mesh, otherwise the tiles get clipped to the tight outline.
        settings.spriteMeshType = isPipeBody || path.Contains("white") ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);

        importer.spriteBorder = FindValue(path, Borders, Vector4.zero);
        importer.SaveAndReimport();
    }

    private static void ApplySfx(string path)
    {
        if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
            return;

        importer.forceToMono = true;
        importer.defaultSampleSettings = new AudioImporterSampleSettings
        {
            loadType = AudioClipLoadType.DecompressOnLoad,
            compressionFormat = AudioCompressionFormat.Vorbis,
            quality = 0.7f,
            sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate
        };
        importer.SaveAndReimport();
    }

    private static void ApplyMusic(string path)
    {
        if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
            return;

        importer.forceToMono = true;
        importer.loadInBackground = true;
        importer.defaultSampleSettings = new AudioImporterSampleSettings
        {
            loadType = AudioClipLoadType.CompressedInMemory,
            compressionFormat = AudioCompressionFormat.Vorbis,
            quality = 0.6f,
            sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate
        };
        importer.SaveAndReimport();
    }

    private static bool MatchesAny(string path, string[] keys)
    {
        foreach (string key in keys)
        {
            if (path.Contains(key))
                return true;
        }
        return false;
    }

    private static T FindValue<T>(string path, Dictionary<string, T> table, T fallback)
    {
        foreach (var pair in table)
        {
            if (path.Contains(pair.Key))
                return pair.Value;
        }
        return fallback;
    }
}
