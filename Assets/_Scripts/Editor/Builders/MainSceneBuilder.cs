using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class MainSceneBuilder
{
    public const float GroundTopY = -3.8f;
    public const float CameraSize = 5f;

    private static readonly Color SkyColor = new(0.835f, 0.929f, 0.969f);       // #D5EDF7, top of the Kenney background
    private static readonly Color GroundFillColor = new(0.592f, 0.443f, 0.290f); // #97714A, bottom of groundGrass
    private static readonly Vector2 BirdStart = new(-1.2f, 0.3f);

    [MenuItem("Tools/Flappy/Builders/Main Scene")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Camera camera = CreateCamera();
        camera.gameObject.AddComponent<CameraShaker>();
        var config = AssetDatabase.LoadAssetAtPath<PipeSpawnConfig>(GameplayPrefabBuilder.ConfigPath);

        var world = new GameObject("World").transform;
        var scroller = new GameObject("LevelScroller").AddComponent<LevelScroller>();
        scroller.transform.SetParent(world, false);
        SetField(scroller, "_config", config);

        BuildBackground(world, scroller);
        BuildGround(world, scroller);

        var spawner = new GameObject("PipeSpawner").AddComponent<PipeSpawner>();
        spawner.transform.SetParent(world, false);
        SetField(spawner, "_config", config);
        SetField(spawner, "_scroller", scroller);
        SetField(spawner, "_camera", camera);

        var birdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPrefabBuilder.BirdPath);
        var bird = (GameObject)PrefabUtility.InstantiatePrefab(birdPrefab, scene);
        bird.transform.position = BirdStart;
        SetField(bird.GetComponent<Bird>(), "_camera", camera);

        var gameManager = new GameObject("GameManager").AddComponent<GameManager>();
        SetField(gameManager, "_spawner", spawner);

        EditorSceneManager.SaveScene(scene, ProjectSettingsBuilder.MainScenePath);
        Debug.Log("MainSceneBuilder: done");
    }

    public static Camera CreateCamera()
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        go.transform.position = new Vector3(0f, 0f, -10f);
        var camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = CameraSize;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = SkyColor;
        go.AddComponent<UniversalAdditionalCameraData>();

        var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        return camera;
    }

    public static void BuildBackground(Transform parent, LevelScroller scroller)
    {
        Sprite sprite = GameplayPrefabBuilder.LoadSprite("Assets/_Art/Kenney/TappyPlane/background.png");
        float width = sprite.bounds.size.x - 0.02f;

        var root = new GameObject("Background").transform;
        root.SetParent(parent, false);
        var segments = new Transform[2];
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i] = GameplayPrefabBuilder.CreateSprite(root, $"Segment{i}", sprite, GameplayPrefabBuilder.BackgroundOrder,
                new Vector3(i * width, 0f, 0f)).transform;
        }

        AddLoop(root.gameObject, segments, width, 0.3f, scroller);
    }

    public static void BuildGround(Transform parent, LevelScroller scroller)
    {
        Sprite strip = GameplayPrefabBuilder.LoadSprite("Assets/_Art/Kenney/TappyPlane/groundGrass.png");
        Sprite white = GameplayPrefabBuilder.LoadSprite("Assets/_Art/Generated/world_white.png");
        float width = strip.bounds.size.x - 0.02f;
        float stripHeight = strip.bounds.size.y;

        // Grass peaks sit between ~0.26 and ~0.68 units above the strip's bottom; the collider top is in between.
        float stripBottom = GroundTopY - 0.45f;
        float fillTop = stripBottom + 0.02f;
        float fillBottom = -CameraSize - 1f;

        var root = new GameObject("Ground").transform;
        root.SetParent(parent, false);
        var segments = new Transform[2];
        for (int i = 0; i < segments.Length; i++)
        {
            var segment = new GameObject($"Segment{i}").transform;
            segment.SetParent(root, false);
            segment.localPosition = new Vector3(i * width, 0f, 0f);

            GameplayPrefabBuilder.CreateSprite(segment, "Grass", strip, GameplayPrefabBuilder.GroundOrder,
                new Vector3(0f, stripBottom + stripHeight * 0.5f, 0f));

            SpriteRenderer fill = GameplayPrefabBuilder.CreateSprite(segment, "Fill", white, GameplayPrefabBuilder.GroundOrder - 1,
                new Vector3(0f, (fillTop + fillBottom) * 0.5f, 0f));
            fill.color = GroundFillColor;
            Vector2 whiteSize = white.bounds.size;
            fill.transform.localScale = new Vector3((width + 0.1f) / whiteSize.x, (fillTop - fillBottom) / whiteSize.y, 1f);
            segments[i] = segment;
        }

        AddLoop(root.gameObject, segments, width, 1f, scroller);

        var collider = new GameObject("GroundCollider").AddComponent<BoxCollider2D>();
        collider.transform.SetParent(root, false);
        collider.size = new Vector2(40f, 3f);
        collider.offset = new Vector2(0f, GroundTopY - 1.5f);
    }

    private static void AddLoop(GameObject go, Transform[] segments, float width, float speedFactor, LevelScroller scroller)
    {
        var loop = go.AddComponent<ScrollingLoop>();
        var serialized = new SerializedObject(loop);
        SerializedProperty array = serialized.FindProperty("_segments");
        array.arraySize = segments.Length;
        for (int i = 0; i < segments.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = segments[i];
        serialized.FindProperty("_segmentWidth").floatValue = width;
        serialized.FindProperty("_speedFactor").floatValue = speedFactor;
        serialized.FindProperty("_scroller").objectReferenceValue = scroller;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetField(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(field);
        if (property == null)
        {
            Debug.LogError($"{target.GetType().Name} has no field {field}");
            return;
        }
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
