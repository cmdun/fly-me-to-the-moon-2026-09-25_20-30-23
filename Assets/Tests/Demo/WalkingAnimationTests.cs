#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FlyMeToTheMoon;
using FlyMeToTheMoon.Demo;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class WalkingAnimationTests
{
    DemoGame game;Keyboard keyboard;InputSettings.BackgroundBehavior background;InputSettings.EditorInputBehaviorInPlayMode editorInput;
    [UnitySetUp] public IEnumerator Setup()
    {
        background=InputSystem.settings.backgroundBehavior;editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/FullDemo.unity",new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;yield return null;game=Object.FindAnyObjectByType<DemoGame>();game.StartGame();
        keyboard=InputSystem.AddDevice<Keyboard>();yield return new WaitForSeconds(.3f);
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Time.timeScale=1;InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=background;
        InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;yield return null;
    }
    void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
    [UnityTest] public IEnumerator RealMovementAlternatesBothFeetInBothDirectionsAroundThePlanet()
    {
        var art=game.Art.PlayerArt;
        foreach(var up in new[]{Vector2.up,Vector2.right,Vector2.down,Vector2.left})
        foreach(var direction in new[]{Key.D,Key.A})
        {
            Keys();game.Player.Respawn(game.Planets.respawnPlanet,up);yield return new WaitForSeconds(.3f);Keys(direction);
            yield return new WaitForSeconds(.3f);bool frontLift=false,backLift=false,frontAhead=false,backAhead=false;var phases=new HashSet<string>();
            float end=Time.time+.9f;
            while(Time.time<end)
            {
                yield return null;
                Assert.IsTrue(art.Walk.Visible,"Walking must animate during real controller movement");
                frontLift|=art.Walk.FrontFoot.y>.04f;backLift|=art.Walk.BackFoot.y>.04f;
                frontAhead|=art.Walk.FrontFoot.x-art.Walk.BackFoot.x>.15f;backAhead|=art.Walk.BackFoot.x-art.Walk.FrontFoot.x>.15f;
                Assert.IsTrue(art.Walk.FrontFoot.y<.001f || art.Walk.BackFoot.y<.001f,"At least one sole stays on the surface");
                Assert.That(Vector2.Dot(art.Traveler.transform.up,game.Player.Up),Is.GreaterThan(.985f));phases.Add(art.Pose);
            }
            Assert.IsTrue(frontLift && backLift && frontAhead && backAhead,"Both legs must lead, plant and lift");Assert.GreaterOrEqual(phases.Count,6);
            Assert.AreEqual(direction==Key.A,art.Traveler.flipX);
        }
        Keys();yield return new WaitForSeconds(.3f);Assert.IsFalse(art.Walk.Visible);Assert.IsTrue(art.Traveler.enabled);Assert.AreEqual("hero/0",art.Pose);
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator WalkingYieldsToShotsJumpingAndPause()
    {
        var art=game.Art.PlayerArt;Keys(Key.D);yield return new WaitForSeconds(.5f);Assert.IsTrue(art.Walk.Visible);
        game.Pause();float phase=art.Walk.Phase;yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(phase,art.Walk.Phase);game.Resume();yield return new WaitForSeconds(.2f);
        game.ShootToward(game.Player.Body.position+game.Player.Up*4);yield return new WaitForSeconds(.1f);
        Assert.IsFalse(art.Walk.Visible);Assert.IsTrue(art.Traveler.enabled);StringAssert.StartsWith("01/actor/",art.Pose);
        Assert.Less(Mathf.Abs(Vector2.Dot(game.Player.Body.linearVelocity,game.Player.transform.right)),.05f);
        yield return new WaitForSeconds(.4f);Assert.IsTrue(art.Walk.Visible);
        Keys(Key.D,Key.Q);yield return new WaitForSeconds(.3f);
        Assert.IsTrue(art.Walk.Visible,"Calling while walking must keep both feet moving");
        StringAssert.Contains("01/actor/",art.Walk.UpperPose);Assert.IsFalse(art.Instrument.enabled);
        Keys(Key.Space,Key.D);yield return new WaitForSeconds(.1f);Assert.IsFalse(art.Walk.Visible);Assert.AreEqual("hero/5",art.Pose);
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator RenderContactAndPassingPoses()
    {
        if(Application.isBatchMode)yield break;
        var art=game.Art.PlayerArt;game.Player.Respawn(game.Planets.respawnPlanet,Vector2.down);yield return new WaitForSeconds(.3f);
        Keys(Key.D);yield return new WaitForSeconds(.5f);Keys();
        game.Player.enabled=false;game.Player.Body.linearVelocity=Vector2.zero;game.Player.Body.simulated=false;art.enabled=false;
        game.Planets.cameraController.enabled=false;Object.FindAnyObjectByType<DemoZoom>().enabled=false;
        Camera.main.transform.position=game.Player.transform.position+game.Player.transform.up*.5f+Vector3.back*10;
        Camera.main.transform.rotation=game.Player.transform.rotation;
        art.Walk.SetVisible(false);
        for(int i=0;i<8;i++)
        {
            art.Walk.Step(i==0?0:DemoWalkCycle.CycleDistance/8,false);
            yield return Capture("walk-right-"+i);
        }
        art.Walk.Step(0,true);yield return Capture("walk-left");
        LogAssert.NoUnexpectedReceived();
    }
    static IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        string folder="/Users/williamxi/Documents/ChatGPT/Hackathon/tmp/walking-views";Directory.CreateDirectory(folder);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return null;yield return null;
    }
}
#endif
