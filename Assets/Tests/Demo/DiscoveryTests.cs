#if UNITY_EDITOR
using System.Collections;
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

public class DiscoveryTests
{
    DemoGame g; Keyboard keyboard; InputSettings.BackgroundBehavior background;InputSettings.EditorInputBehaviorInPlayMode editorInput;
    [UnitySetUp] public IEnumerator Setup()
    {
        background=InputSystem.settings.backgroundBehavior;editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/FullDemo.unity",new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;yield return null;g=Object.FindAnyObjectByType<DemoGame>();g.StartGame();
        keyboard=InputSystem.AddDevice<Keyboard>();yield return new WaitForSeconds(.25f);
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        Time.timeScale=1;InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=background;
        InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;yield return null;
    }
    IEnumerator Stand(MoonDiscovery site,float arc)
    {g.Player.Respawn(site.Body,site.At(arc)-site.Body.Center);g.Planets.cameraController.SnapToPlayer();yield return new WaitForSeconds(.2f);}
    IEnumerator Read()
    {while(g.State==DemoState.Dialogue){g.Hud.RevealDialogue();g.AdvanceDialogue();yield return null;}}
    IEnumerator Capture(string name)
    {
        if(Application.isBatchMode)yield break;
        yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();
        string folder="/Users/williamxi/Documents/ChatGPT/Hackathon/tmp/discovery-views";Directory.CreateDirectory(folder);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return null;
    }
    [UnityTest] public IEnumerator ActivitiesCoverTwelveDistinctNonQuestMoonsAndJournalPauses()
    {
        var moons=new System.Collections.Generic.HashSet<int>();int[] kinds=new int[3];
        foreach(var site in g.Discoveries.Sites)
        {
            Assert.IsTrue(moons.Add(site.Moon));kinds[site.Kind]++;
            foreach(var station in g.Melody.Stations)Assert.AreNotEqual(station.Body,site.Body);
            foreach(var control in site.Controls)Assert.That(Vector2.Distance(control.position,site.Body.Center),Is.GreaterThanOrEqualTo(site.Body.radius-.02f));
            Assert.IsFalse(g.Discoveries.Award(site));
        }
        CollectionAssert.AreEqual(new[]{4,4,4},kinds);
        var bell=g.Discoveries.Sites[0];yield return Stand(bell,-4.2f);g.Interact();yield return Read();yield return new WaitForSeconds(1);
        Assert.GreaterOrEqual(Camera.main.orthographicSize,8f,"Frame the whole activity during its demonstration");
        g.ToggleJournal();Assert.AreEqual(DemoState.Journal,g.State);Assert.AreEqual(0,Time.timeScale);Assert.IsFalse(g.Player.enabled);
        yield return Capture("journal");g.ToggleJournal();Assert.AreEqual(1,Time.timeScale);
        g.Discoveries.Track(4);g.ToggleMap();yield return Capture("discovery-map");g.ToggleMap();
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator BellGardensRequireTheWholePhraseAndRejectEchoAndRepeatRewards()
    {
        foreach(var s in g.Discoveries.Sites)
        {
            if(s.Kind!=0)continue;
            yield return Stand(s,-4.2f);g.Interact();yield return Read();
            Assert.IsTrue(s.Listening);yield return new WaitForSeconds(.5f+s.Melody.Length*.85f);
            Assert.IsFalse(s.Listening);
            int wrong=(s.Melody[0]+1)%3;
            Vector2 target=s.At(-2.3f+wrong*2.3f,.8f);
            Assert.IsFalse(g.Discoveries.NotePassed(target-Vector2.one,target+Vector2.one,true));Assert.AreEqual(0,s.Progress);
            Assert.IsTrue(g.Discoveries.NotePassed(target-Vector2.one*.1f,target+Vector2.one*.1f,false));Assert.AreEqual(0,s.Progress);
            yield return new WaitForSeconds(.24f);
            // Aim real projectiles at each bell; only the player's starting position is arranged by the test.
            for(int n=0;n<s.Melody.Length;n++)
            {
                int bell=s.Melody[n];float arc=-2.3f+bell*2.3f;
                yield return Stand(s,arc-.8f);g.ShootToward(s.At(arc,.8f));yield return new WaitForSeconds(.5f);
                Assert.AreEqual(n+1,s.Progress,"Moon "+s.Moon+" note "+n);
            }
            Assert.IsTrue(s.Complete);Assert.IsFalse(g.Discoveries.Award(s));
            yield return Capture("bell-garden-"+s.Rank);
        }
        Assert.AreEqual(4,g.Discoveries.FoundCount);Assert.AreEqual(0,g.Melody.Count);
    }
    [UnityTest] public IEnumerator ReflectorsNeedAnUnbrokenBeamAndAVisitToTheReceiver()
    {
        foreach(var s in g.Discoveries.Sites)
        {
            if(s.Kind!=1)continue;
            yield return Stand(s,4.25f);g.Interact();yield return Read();g.Interact();Assert.IsFalse(s.Complete);
            for(int i=0;i<s.Turns.Length;i++)
            {
                yield return Stand(s,s.Arc(s.Controls[i+1].position));
                int limit=0;
                while(s.Turns[i]!=s.CorrectTurns[i] && limit++<4)g.Interact();
                Assert.AreEqual(s.CorrectTurns[i],s.Turns[i]);
                Assert.AreEqual(i+1,s.LitMirrors);
            }
            Assert.IsFalse(s.Complete,"Light alone does not grant a remote reward");
            yield return Capture("prism-"+s.Rank);
            yield return Stand(s,4.25f);g.Interact();Assert.IsTrue(s.Complete);Assert.IsFalse(g.Discoveries.Award(s));
        }
        Assert.AreEqual(4,g.Discoveries.FoundCount);
    }
    [UnityTest] public IEnumerator LanternWalksNeedFoodEveryRestStopAndCloseGroundedGuidance()
    {
        foreach(var s in g.Discoveries.Sites)
        {
            if(s.Kind!=2)continue;
            yield return Stand(s,-3.5f);g.Interact();yield return Read();g.Interact();Assert.IsFalse(s.Fed);
            yield return Stand(s,4.25f);g.Interact();Assert.IsTrue(s.HasFood);Assert.IsFalse(s.Complete);
            yield return Stand(s,-3.5f);g.Interact();Assert.IsTrue(s.Fed);
            yield return Stand(s,4.25f);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Q));yield return new WaitForSeconds(.4f);
            Assert.That(s.CreatureArc,Is.EqualTo(-3.5f).Within(.01f));Assert.AreEqual(0,s.Shelters);
            float timeout=Time.time+30;
            while(!s.Complete && Time.time<timeout)
            {
                // Follow the ground using the ordinary controller, staying roughly one metre ahead of the singer.
                float desired=s.CreatureArc+1.2f;
                float delta=desired-s.Arc(g.Player.Body.position);
                // The first relocation only returns from the explicit bypass attempt above.
                if(Mathf.Abs(delta)>5){yield return Stand(s,desired);continue;}
                Key direction=delta>0?Key.D:Key.A;
                InputSystem.QueueStateEvent(keyboard,Mathf.Abs(delta)>.18f?new KeyboardState(Key.Q,direction):new KeyboardState(Key.Q));
                yield return null;
            }
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            Assert.IsTrue(s.Complete,"Lantern walk rank "+s.Rank+" stalled at "+s.CreatureArc+"; player "+s.Arc(g.Player.Body.position));
            Assert.AreEqual(3,s.Shelters);yield return Capture("lantern-"+s.Rank);
        }
        Assert.AreEqual(4,g.Discoveries.FoundCount);
    }
    [UnityTest] public IEnumerator GardenDeliveryAndOldSavesAreSafeAndEncoreIsReplayable()
    {
        var d=g.Discoveries;
        var old=JsonUtility.FromJson<DemoSave.Data>("{\"version\":1,\"fragments\":[true,false,false,false,false],\"moon\":0,\"upY\":1}");
        Assert.IsTrue(old.Valid);d.Restore(old);Assert.AreEqual(0,d.FoundCount);
        var invalid=new DemoSave.Data{discoveries=new[]{true,false},garden=new[]{true,true},festival=true,bestPerformance=999};
        d.Restore(invalid);Assert.AreEqual(1,d.FoundCount);Assert.AreEqual(1,d.HomeCount);Assert.IsFalse(d.Festival);Assert.AreEqual(24,d.BestPerformance);
        // Arrange a completed expedition to verify its home-side persistence and festival gates.
        var data=new DemoSave.Data{discoveries=new bool[12],garden=new bool[12]};
        for(int i=0;i<12;i++)data.discoveries[i]=true;
        d.Restore(JsonUtility.FromJson<DemoSave.Data>(JsonUtility.ToJson(data)));
        Assert.IsFalse(d.Deliver(),"Rewards cannot be delivered remotely");
        g.Player.Respawn(g.Planets.respawnPlanet,(Vector2)d.Garden.position-g.Planets.respawnPlanet.Center);
        g.Planets.cameraController.SnapToPlayer();yield return new WaitForSeconds(.3f);
        Assert.IsTrue(d.Deliver());Assert.AreEqual(12,d.HomeCount);Assert.IsFalse(d.Deliver());
        Assert.IsTrue(d.HomeAwakened);Assert.AreEqual(1,d.HomeEvents);
        Assert.AreSame(d.Garden,d.Observatory);StringAssert.Contains("Observatory",d.Observatory.name);
        d.PerformanceFinished(false,6);Assert.IsFalse(d.Festival);
        d.PerformanceFinished(true,17);Assert.IsFalse(d.Festival,"The restored score is still required");
        g.Melody.Unlocked=true;d.PerformanceFinished(true,17);Assert.IsTrue(d.Festival);Assert.AreEqual(17,d.BestPerformance);
        yield return Capture("full-home-garden");
        for(int i=0;i<5;i++)g.Melody.Fragments[i]=true;
        typeof(DemoGame).GetProperty("MelodyRepaired").SetValue(g,true);
        g.Melody.Unlocked=true;typeof(DemoGame).GetProperty("Completed").SetValue(g,1);
        g.Player.Respawn(g.Planets.respawnPlanet,g.Melody.Altar-g.Planets.respawnPlanet.Center);yield return new WaitForSeconds(.2f);
        Assert.IsTrue(g.BeginRhythm(g.Melody),"Restored scores remain replayable after the piano unlock");
        g.Pause();g.Resume();Assert.AreEqual(DemoState.Rhythm,g.State);g.Continue();
        LogAssert.NoUnexpectedReceived();
    }
}
#endif
