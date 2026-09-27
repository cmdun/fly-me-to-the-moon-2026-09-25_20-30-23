#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opt-in Editor-only launch helper; never builds an application or runs during normal editing.</summary>
public static class PolishValidation
{
    [InitializeOnLoadMethod]
    static void Initialize()
    {
        var args=Environment.GetCommandLineArgs();
        if(Array.IndexOf(args,"-polishPlay")<0)return;
        if(Array.IndexOf(args,"-polishExit")>=0)EditorApplication.update+=FinishValidation;
        if(SessionState.GetBool("PolishValidation.started",false))return;
        EditorApplication.update+=StartWhenReady;
    }
    static void FinishValidation()
    {
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-demoSmoke");
        if(index<0 || index+1>=args.Length)return;
        bool failed=File.Exists(Path.Combine(args[index+1],"FAILED.txt"));
        if(!failed && !File.Exists(Path.Combine(args[index+1],"result.json")))return;
        if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)return;
        EditorApplication.update-=FinishValidation;
        if(SceneManager.GetActiveScene().isDirty){Debug.LogWarning("Validation complete; unsaved Editor scene left open.");return;}
        EditorApplication.Exit(failed?1:0);
    }
    static void StartWhenReady()
    {
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup<8)return;
        EditorApplication.update-=StartWhenReady;
        if(SceneManager.GetActiveScene().isDirty){Debug.LogError("Polish validation left the unsaved scene untouched.");return;}
        SessionState.SetBool("PolishValidation.started",true);
        EditorSceneManager.OpenScene(FullDemoBuilder.ScenePath);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.isPlaying=true;
    }
}
#endif
