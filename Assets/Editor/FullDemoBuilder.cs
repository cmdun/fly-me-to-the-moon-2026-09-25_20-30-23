using System;
using System.IO;
using FlyMeToTheMoon;
using FlyMeToTheMoon.Demo;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class FullDemoBuilder
{
    public const string ScenePath = "Assets/Scenes/FullDemo.unity";
    public static void ImportTextResources()
    {
        AssetDatabase.importPackageCompleted += _ => { AssetDatabase.Refresh(); EditorApplication.delayCall += () => EditorApplication.Exit(0); };
        TMP_PackageResourceImporter.ImportResources(true, false, false);
    }
    [MenuItem("Tools/Fly Me to the Moon/Create Full Local Demo")]
    public static void Create()
    {
        var pipeline = GraphicsSettings.currentRenderPipeline;
        if (pipeline == null || !pipeline.GetType().FullName.Contains("Universal")) throw new Exception("Demo requires the existing URP pipeline.");
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode first.");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null) throw new Exception("Run FullDemoBuilder.ImportTextResources first, then reopen for asset import.");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/FirstPlayablePrototype.unity");
        var root = new GameObject("Full Demo");
        var game = root.AddComponent<DemoGame>();
        game.Font = font;
        game.Material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prototype/FlatColor.mat");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.productName = "Fly Me to the Moon Demo";
        PlayerSettings.companyName = "Local Hackathon";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.localhackathon.flymetothemoon.demo");
        PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
        AssetDatabase.SaveAssets();
        Debug.Log("Full local demo scene created.");
    }
    [MenuItem("Tools/Fly Me to the Moon/Play Full Local Demo")]
    public static void Play()
    {
        if (EditorApplication.isPlaying) return;
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.isPlaying = true;
    }
    public static void BuildMac()
    {
        string product = PlayerSettings.productName, company = PlayerSettings.companyName;
        var target = UnityEditor.Build.NamedBuildTarget.Standalone;
        string identifier = PlayerSettings.GetApplicationIdentifier(target);
        int width = PlayerSettings.defaultScreenWidth, height = PlayerSettings.defaultScreenHeight;
        var mode = PlayerSettings.fullScreenMode; bool background = PlayerSettings.runInBackground;
        try
        {
            PlayerSettings.productName = "Fly Me to the Moon Demo";
            PlayerSettings.companyName = "Local Hackathon";
            PlayerSettings.SetApplicationIdentifier(target, "com.localhackathon.flymetothemoon.rings");
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.runInBackground = true;
            string path = Path.GetFullPath("Build/Fly Me to the Moon Demo.app");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = path, target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Demo build failed: " + report.summary.result);
            Debug.Log("DEMO_BUILD_SUCCESS " + path);
        }
        finally
        {
            PlayerSettings.productName = product; PlayerSettings.companyName = company;
            PlayerSettings.SetApplicationIdentifier(target, identifier);
            PlayerSettings.defaultScreenWidth = width; PlayerSettings.defaultScreenHeight = height;
            PlayerSettings.fullScreenMode = mode; PlayerSettings.runInBackground = background;
        }
    }
}
