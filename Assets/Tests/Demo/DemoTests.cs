#if UNITY_EDITOR
using System.Collections;
using FlyMeToTheMoon;
using FlyMeToTheMoon.Demo;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class DemoTests
{
    [Test] public void TimingWindowsWrongLanesAndDuplicateHits()
    {
        var r=new DemoRhythm();r.Begin(0,0);
        Assert.AreEqual("Keep the beat",r.Hit(1,2));
        Assert.AreEqual("PERFECT",r.Hit(0,2.1));r.Hit(0,2);Assert.AreEqual(1,r.Hits);
        Assert.AreEqual("GOOD",r.Hit(r.Lane(1),r.TimeOf(1)+.2));
        r.Hit(r.Lane(2),r.TimeOf(2)+.201);Assert.AreEqual(2,r.Hits);
        Assert.IsTrue(r.Tick(r.Duration));Assert.AreEqual(22,r.Misses);Assert.IsFalse(r.Passed);
        r.Begin(8,1);Assert.AreEqual(0,r.Hits);Assert.AreEqual(0,r.Misses);
    }
    [Test] public void PassingRequiresSeventeenOfTwentyFourNotes()
    {
        var r=new DemoRhythm();r.Begin(0,0);
        for(int i=0;i<16;i++)r.Hit(r.Lane(i),r.TimeOf(i));
        Assert.IsFalse(r.Passed);r.Hit(r.Lane(16),r.TimeOf(16));Assert.IsTrue(r.Passed);
    }
    [Test] public void ThousandGalaxySeedsHaveThreeNonoverlappingReachableRings()
    {
        for(int seed=1;seed<=1000;seed++)
        {
            var moons=DemoGalaxy.Layout(seed);Assert.AreEqual(18,moons.Length);
            for(int i=0;i<moons.Length;i++)
            {
                Assert.AreEqual(i/6,moons[i].Ring);
                Assert.That(moons[i].Center.magnitude,Is.EqualTo(16.5f+5.5f*(i/6)).Within(.001f));
                for(int j=0;j<i;j++)Assert.Greater(Vector2.Distance(moons[i].Center,moons[j].Center),moons[i].Radius+moons[j].Radius+1);
                float gap=i<6?moons[i].Center.magnitude-12-moons[i].Radius:Vector2.Distance(moons[i].Center,moons[i-6].Center)-moons[i].Radius-moons[i-6].Radius;
                Assert.Less(gap,3.5f,"Each moon needs a short inward connection");
            }
        }
        Assert.AreNotEqual(DemoGalaxy.Layout(1)[0].Center,DemoGalaxy.Layout(2)[0].Center);
        Assert.AreEqual(DemoGalaxy.Layout(3)[8].Center,DemoGalaxy.Layout(3)[8].Center);
    }
    [Test] public void SevenNotesCoverAMajorScale()
    {
        Assert.That(DemoAudio.FrequencyForNote(0),Is.EqualTo(261.6256f).Within(.01));
        Assert.That(DemoAudio.FrequencyForNote(6),Is.EqualTo(493.8833f).Within(.01));
        for(int i=1;i<7;i++)Assert.Greater(DemoAudio.FrequencyForNote(i),DemoAudio.FrequencyForNote(i-1));
    }
}

public class DemoJourneyTests
{
    DemoGame g; Keyboard keyboard;
    InputSettings.BackgroundBehavior previousBackground;
    InputSettings.EditorInputBehaviorInPlayMode previousEditor;
    [UnitySetUp] public IEnumerator SetUp()
    {
        previousBackground=InputSystem.settings.backgroundBehavior;
        previousEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/FullDemo.unity",new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;yield return null;
        g=Object.FindAnyObjectByType<DemoGame>();Assert.NotNull(g);
        keyboard=InputSystem.AddDevice<Keyboard>();g.StartGame();yield return new WaitForSeconds(.2f);
    }
    [UnityTearDown] public IEnumerator TearDown()
    {
        Time.timeScale=1;InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior=previousBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode=previousEditor;
        yield return null;
    }
    IEnumerator Press(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));yield return null;yield return new WaitForFixedUpdate();}
    IEnumerator Land(float timeout=8)
    {
        float end=Time.time+timeout;while(!g.Player.IsGrounded && Time.time<end)yield return null;
        Assert.IsTrue(g.Player.IsGrounded,"Landing timeout");yield return null;
    }
    [UnityTest] public IEnumerator FluteJumpIsLowerAndAirShotConsumesTheOnlyBoost()
    {
        float peak=0;yield return Press(Key.Space);yield return Press();
        while(!g.Player.IsGrounded){peak=Mathf.Max(peak,g.Planets.respawnPlanet.SurfaceDistance(g.Player.Body.position)-.3f);yield return null;}
        Assert.That(peak,Is.InRange(1.1f,1.8f));
        int boosts=g.Player.FluteJumpCount;
        g.ShootToward(g.Player.Body.position+Vector2.up*5);Assert.AreEqual(boosts,g.Player.FluteJumpCount);
        yield return new WaitForSeconds(.25f);yield return Press(Key.Space);yield return Press();
        g.ShootToward(g.Player.Body.position+Vector2.right*5);
        Assert.Less(g.Player.Body.linearVelocity.x,-4.5f);Assert.IsFalse(g.Player.CanFluteJump);Assert.AreEqual(boosts+1,g.Player.FluteJumpCount);
        int shots=g.Shots;yield return new WaitForSeconds(.25f);g.ShootToward(g.Player.Body.position+Vector2.left*5);
        yield return Press(Key.Space);yield return Press();Assert.AreEqual(shots,g.Shots);Assert.AreEqual(boosts+1,g.Player.FluteJumpCount);
        g.Player.Respawn(g.Planets.respawnPlanet,Vector2.up);yield return Land();
        yield return Press(Key.Space);yield return Press();Assert.IsTrue(g.Player.CanFluteJump);
        yield return Press(Key.Space);yield return Press();shots=g.Shots;
        g.ShootToward(g.Player.Body.position+Vector2.down*5);Assert.AreEqual(shots,g.Shots,"Space boost must also consume airborne shot boost");
    }
    [UnityTest] public IEnumerator InwardAndSidewaysRecoilCanLandBeforeClearingLaunchZone()
    {
        var home=g.Planets.respawnPlanet;
        foreach(var aim in new[]{Vector2.up,Vector2.right})
        {
            g.Player.Respawn(home,Vector2.up);yield return Land();yield return new WaitForSeconds(.25f);
            int recoveries=g.Planets.RespawnCount;
            yield return Press(Key.Space);yield return Press();
            g.ShootToward(g.Player.Body.position+aim*10);
            Assert.IsTrue(g.Player.FluteUsed);yield return Land(4);
            Assert.AreEqual(home,g.Planets.CurrentPlanet);Assert.AreEqual(recoveries,g.Planets.RespawnCount);
            Assert.IsFalse(g.Player.FluteUsed);yield return Press(Key.Space);yield return Press();Assert.IsTrue(g.Player.CanFluteJump);
        }
    }
    [UnityTest] public IEnumerator EveryGeneratedMoonCanBeReachedUsingTheStarterFlute()
    {
        var bodies=g.Player.gravityManager.bodies;Assert.AreEqual(19,bodies.Length);
        for(int i=1;i<bodies.Length;i++)
        {
            var origin=i<=6?bodies[0]:bodies[i-6];var target=bodies[i];
            Vector2 direction=(target.Center-origin.Center).normalized;
            g.Player.Respawn(origin,direction);yield return Land();
            yield return Press(Key.Space);yield return Press();yield return Press(Key.Space);yield return Press();
            float end=Time.time+8;
            while(!g.Player.IsGrounded && Time.time<end)
            {
                Vector2 steer=(target.Center-g.Player.Body.position).normalized;
                var keys=new System.Collections.Generic.List<Key>();
                if(Mathf.Abs(steer.x)>.25f)keys.Add(steer.x>0?Key.D:Key.A);
                if(Mathf.Abs(steer.y)>.25f)keys.Add(steer.y>0?Key.W:Key.S);
                yield return Press(keys.ToArray());
            }
            yield return Press();
            Assert.IsTrue(g.Player.IsGrounded,"Moon "+i+" landing");Assert.AreEqual(target,g.Planets.CurrentPlanet,"Moon "+i+" route");
        }
    }
    [UnityTest] public IEnumerator ScatteredPagesTypedDialogueHomeRepairRetryAndPianoUnlock()
    {
        var q=g.Melody;Assert.AreEqual(2,DemoGame.Instruments.Length);
        Assert.AreNotEqual(q.FragmentBodies[0],q.FragmentBodies[1]);Assert.AreNotEqual(q.FragmentBodies[1],q.FragmentBodies[2]);
        Assert.AreEqual(g.Planets.respawnPlanet,q.AltarTarget.Body);
        g.Interact();Assert.AreEqual(DemoState.Dialogue,g.State);Assert.IsFalse(g.Hud.DialogueComplete);
        Vector2 before=g.Player.Body.position;yield return new WaitForSecondsRealtime(.15f);
        Assert.IsFalse(g.Hud.DialogueComplete);Assert.AreEqual(before,g.Player.Body.position);
        g.AdvanceDialogue();Assert.IsTrue(g.Hud.DialogueComplete);g.AdvanceDialogue();Assert.AreEqual(DemoState.Explore,g.State);
        Assert.IsFalse(g.BeginRhythm(q));
        // Pages may be collected in any order. Shoot the middle-ring page first.
        g.Player.Respawn(q.FragmentBodies[1],(Vector2)q.Shot.transform.position-q.FragmentBodies[1].Center);yield return Land();yield return new WaitForSeconds(.2f);
        g.ShootToward(q.Shot.transform.position);yield return new WaitForSeconds(.3f);Assert.IsTrue(q.Fragments[1]);
        g.Player.Respawn(q.FragmentBodies[0],(Vector2)q.Walk.transform.position-q.FragmentBodies[0].Center);yield return Land();Assert.IsTrue(q.Fragments[0]);
        Assert.IsFalse(g.Collect(q,0));Assert.IsFalse(g.Collect(q,2));
        g.Player.Respawn(q.FragmentBodies[2],q.Pedestal-q.FragmentBodies[2].Center);yield return Land();
        g.Interact();Assert.AreEqual(DemoState.Dialogue,g.State);g.AdvanceDialogue();g.AdvanceDialogue();
        yield return new WaitForSeconds(2);Assert.IsFalse(q.PlayingSequence);
        g.HitTarget(q.Resonators[1]);g.HitTarget(q.Resonators[2]);Assert.AreEqual(0,q.SequenceStep);
        foreach(int i in new[]{1,0,2}){g.ShootToward(q.Resonators[i].transform.position);yield return new WaitForSeconds(.4f);}
        yield return null;Assert.IsTrue(q.PuzzleSolved);Assert.AreEqual(3,q.Count);
        Assert.IsFalse(g.BeginRhythm(q),"No performance away from home");Assert.AreEqual(0,g.Completed);
        g.Player.Respawn(g.Planets.respawnPlanet,Vector2.up);yield return Land();
        g.Interact();g.CloseDialogue();Assert.IsFalse(g.MelodyRepaired,"Cancel cannot repair score");
        g.Interact();g.AdvanceDialogue();g.AdvanceDialogue();Assert.IsTrue(g.MelodyRepaired);Assert.AreEqual(DemoState.Dialogue,g.State);
        g.AdvanceDialogue();g.AdvanceDialogue();Assert.AreEqual(DemoState.Rhythm,g.State);
        g.Pause();yield return null;g.Resume();Assert.AreEqual(0,g.Rhythm.Hits);
        while(g.State==DemoState.Rhythm)yield return null;
        Assert.IsFalse(g.LastPassed);Assert.AreEqual(0,g.Completed);Assert.AreEqual(3,q.Count);
        Assert.IsTrue(g.BeginRhythm(q));
        for(int i=0;i<DemoRhythm.NoteCount;i++){while(g.SongTime<g.Rhythm.TimeOf(i))yield return null;g.PlayLane(g.Rhythm.Lane(i));}
        while(g.State==DemoState.Rhythm)yield return null;
        Assert.IsTrue(g.LastPassed);Assert.AreEqual(DemoState.Win,g.State);Assert.AreEqual(1,g.Completed);Assert.AreEqual(1,g.Equipped);
        Assert.AreEqual(6,g.Player.jumpSpeed);Assert.AreEqual(5,g.Player.fluteBoost);
        g.Continue();yield return Land();for(int i=0;i<7;i++)g.FreeNote(i);
        g.CycleInstrument();Assert.AreEqual(0,g.Equipped);Assert.AreEqual(5,g.Player.jumpSpeed);
        g.CycleInstrument();Assert.AreEqual(1,g.Equipped);g.ToggleAudio();Assert.IsTrue(g.Audio.Muted);
        int previousSeed=g.Seed;g.Restart();yield return null;yield return null;
        var fresh=Object.FindAnyObjectByType<DemoGame>();Assert.AreEqual(DemoState.Title,fresh.State);Assert.AreEqual(0,fresh.Melody.Count);Assert.AreEqual(0,fresh.Completed);
        Assert.AreNotEqual(previousSeed,fresh.Seed);
    }
}
#endif
