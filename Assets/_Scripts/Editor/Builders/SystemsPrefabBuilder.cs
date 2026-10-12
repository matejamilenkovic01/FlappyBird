using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

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
        var root = new GameObject("Systems");

        BuildAudio(root.transform);
        CreateChild<HapticsService>(root.transform, "HapticsService");
        CreateChild<InputRouter>(root.transform, "InputRouter");
        BuildEventSystem(root.transform);

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
        go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
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
