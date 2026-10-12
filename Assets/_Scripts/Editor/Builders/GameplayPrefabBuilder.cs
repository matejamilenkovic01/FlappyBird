using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GameplayPrefabBuilder
{
    public const string PrefabFolder = "Assets/_Prefabs/Gameplay";
    public const string BirdPath = PrefabFolder + "/Bird.prefab";
    public const string PipeNormalPath = PrefabFolder + "/PipePair_Normal.prefab";
    public const string PipeBonusPath = PrefabFolder + "/PipePair_Bonus.prefab";
    public const string ConfigFolder = "Assets/_ScriptableObjects";
    public const string ConfigPath = ConfigFolder + "/PipeSpawnConfig.asset";

    public const int BackgroundOrder = -10;
    public const int PipeOrder = 0;
    public const int GroundOrder = 10;
    public const int BirdOrder = 20;

    private const float PipeLength = 10f;
    private const float CapHeight = 0.5f;
    private const float BodyColliderWidth = 0.94f;
    private const float CapColliderWidth = 1.18f;
    private const float BirdColliderRadius = 0.27f;

    [MenuItem("Tools/Flappy/Builders/Gameplay Prefabs")]
    public static void Build()
    {
        EnsureFolder("Assets/_Prefabs", "Gameplay");
        EnsureFolder("Assets", "_ScriptableObjects");

        PipePair normal = BuildPipe(PipeNormalPath, "green", false);
        PipePair bonus = BuildPipe(PipeBonusPath, "gold", true);
        BuildBird();
        BuildConfig(normal, bonus);

        AssetDatabase.SaveAssets();
        Debug.Log("GameplayPrefabBuilder: done");
    }

    private static PipePair BuildPipe(string path, string color, bool pulse)
    {
        Sprite body = LoadSprite($"Assets/_Art/Generated/pipe_body_{color}.png");
        Sprite cap = LoadSprite($"Assets/_Art/Generated/pipe_cap_{color}.png");

        var root = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
        var rigidbody = root.AddComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

        // The cap sprite is drawn for the bottom pipe (lip shadow at its lower edge), so the top pipe flips it.
        Transform top = BuildPipeHalf(root.transform, "Top", body, cap, 1f, out Transform topCap);
        Transform bottom = BuildPipeHalf(root.transform, "Bottom", body, cap, -1f, out Transform bottomCap);

        var scoreZoneObject = new GameObject("ScoreZone");
        scoreZoneObject.transform.SetParent(root.transform, false);
        scoreZoneObject.transform.localPosition = new Vector3(0.6f, 0f, 0f);
        var scoreZone = scoreZoneObject.AddComponent<BoxCollider2D>();
        scoreZone.isTrigger = true;
        scoreZone.size = new Vector2(0.1f, 2.7f);

        PipePair pipePair = root.AddComponent<PipePair>();
        var serialized = new SerializedObject(pipePair);
        serialized.FindProperty("_top").objectReferenceValue = top;
        serialized.FindProperty("_bottom").objectReferenceValue = bottom;
        serialized.FindProperty("_scoreZone").objectReferenceValue = scoreZone;
        SerializedProperty targets = serialized.FindProperty("_pulseTargets");
        targets.arraySize = pulse ? 2 : 0;
        if (pulse)
        {
            targets.GetArrayElementAtIndex(0).objectReferenceValue = topCap;
            targets.GetArrayElementAtIndex(1).objectReferenceValue = bottomCap;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();

        SetLayerRecursive(root, ProjectSettingsBuilder.PipeLayer);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<PipePair>();
    }

    private static Transform BuildPipeHalf(Transform parent, string name, Sprite body, Sprite cap, float direction, out Transform capTransform)
    {
        var half = new GameObject(name).transform;
        half.SetParent(parent, false);
        bool flip = direction > 0f;

        var bodyRenderer = CreateSprite(half, "Body", body, PipeOrder, new Vector3(0f, direction * (CapHeight + PipeLength * 0.5f), 0f));
        bodyRenderer.drawMode = SpriteDrawMode.Tiled;
        bodyRenderer.tileMode = SpriteTileMode.Continuous;
        bodyRenderer.size = new Vector2(1f, PipeLength);
        bodyRenderer.flipY = flip;

        var capRenderer = CreateSprite(half, "Cap", cap, PipeOrder + 1, new Vector3(0f, direction * CapHeight * 0.5f, 0f));
        capRenderer.flipY = flip;
        capTransform = capRenderer.transform;

        var bodyCollider = half.gameObject.AddComponent<BoxCollider2D>();
        bodyCollider.size = new Vector2(BodyColliderWidth, PipeLength);
        bodyCollider.offset = new Vector2(0f, direction * (CapHeight + PipeLength * 0.5f));

        var capCollider = half.gameObject.AddComponent<BoxCollider2D>();
        capCollider.size = new Vector2(CapColliderWidth, CapHeight - 0.04f);
        capCollider.offset = new Vector2(0f, direction * CapHeight * 0.5f);

        return half;
    }

    private static void BuildBird()
    {
        var root = new GameObject("Bird");
        var rigidbody = root.AddComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Dynamic;
        rigidbody.gravityScale = 2.2f;
        rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

        var collider = root.AddComponent<CircleCollider2D>();
        collider.radius = BirdColliderRadius;

        SpriteRenderer visual = CreateSprite(root.transform, "Visual", LoadSprite("Assets/_Art/Kenney/AnimalPack/parrot.png"), BirdOrder, Vector3.zero);

        Bird bird = root.AddComponent<Bird>();
        root.AddComponent<BirdInput>();

        var serialized = new SerializedObject(bird);
        serialized.FindProperty("_visual").objectReferenceValue = visual.transform;
        serialized.FindProperty("_ignoreWhenDead").intValue = 1 << ProjectSettingsBuilder.PipeLayer;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, BirdPath);
        Object.DestroyImmediate(root);
    }

    private static void BuildConfig(PipePair normal, PipePair bonus)
    {
        var config = AssetDatabase.LoadAssetAtPath<PipeSpawnConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<PipeSpawnConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }

        config.PipeTypes = new List<PipeTypeEntry>
        {
            new() { Id = "Normal", Prefab = normal, SpawnWeight = 0.85f, ScoreValue = 1 },
            new() { Id = "Bonus", Prefab = bonus, SpawnWeight = 0.15f, ScoreValue = 2 }
        };
        EditorUtility.SetDirty(config);
    }

    public static SpriteRenderer CreateSprite(Transform parent, string name, Sprite sprite, int order, Vector3 localPosition)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return renderer;
    }

    public static Sprite LoadSprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogError($"Missing sprite: {path}");
        return sprite;
    }

    public static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent))
        {
            int slash = parent.LastIndexOf('/');
            EnsureFolder(parent[..slash], parent[(slash + 1)..]);
        }

        if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
