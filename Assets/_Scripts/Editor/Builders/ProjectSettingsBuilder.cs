using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProjectSettingsBuilder
{
    public const string ScenesFolder = "Assets/_Scenes";
    public const string MenuScenePath = ScenesFolder + "/Menu.unity";
    public const string MainScenePath = ScenesFolder + "/Main.unity";

    private const string SampleSceneFolder = "Assets/Scenes";

    [MenuItem("Tools/Flappy/Builders/Project Settings")]
    public static void Apply()
    {
        PlayerSettings.companyName = "Mateja Milenkovic";
        PlayerSettings.productName = "Flappy Bird";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.matejamilenkovic.flappybird");
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.Android.bundleVersionCode = 1;

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        EnsureScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("ProjectSettingsBuilder: done");
    }

    private static void EnsureScenes()
    {
        if (!AssetDatabase.IsValidFolder(ScenesFolder))
            AssetDatabase.CreateFolder("Assets", "_Scenes");

        CreateEmptySceneIfMissing(MenuScenePath);
        CreateEmptySceneIfMissing(MainScenePath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(MainScenePath, true)
        };

        if (AssetDatabase.IsValidFolder(SampleSceneFolder))
        {
            if (EditorSceneManager.GetActiveScene().path.StartsWith(SampleSceneFolder))
                EditorSceneManager.OpenScene(MainScenePath);

            AssetDatabase.DeleteAsset(SampleSceneFolder);
        }
    }

    private static void CreateEmptySceneIfMissing(string path)
    {
        if (File.Exists(path))
            return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.82f, 0.93f, 0.97f);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        SceneManager.MoveGameObjectToScene(cameraObject, scene);

        EditorSceneManager.SaveScene(scene, path);
        EditorSceneManager.CloseScene(scene, true);
    }
}
