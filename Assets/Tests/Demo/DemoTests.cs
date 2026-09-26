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
    [Test] public void ThousandSeedsHaveLargeVariedMoonsAndSpacedConnectedRoutes()
    {
        for(int seed=1;seed<=1000;seed++)
        {
            var moons=DemoGalaxy.Layout(seed);Assert.AreEqual(24,moons.Length);
            float outer=0,smallest=10,largest=0;
            for(int i=0;i<moons.Length;i++)
            {
                Assert.AreEqual(i/6,moons[i].Ring);Assert.That(moons[i].Radius,Is.InRange(3.2f,5.8f));
                smallest=Mathf.Min(smallest,moons[i].Radius);largest=Mathf.Max(largest,moons[i].Radius);
                outer=Mathf.Max(outer,moons[i].Center.magnitude+moons[i].Radius);
                Assert.GreaterOrEqual(moons[i].Center.magnitude-12-moons[i].Radius,6.499f);
                for(int j=0;j<i;j++)Assert.GreaterOrEqual(Vector2.Distance(moons[i].Center,moons[j].Center)-moons[i].Radius-moons[j].Radius,6.499f);
                float gap=i<6?moons[i].Center.magnitude-12-moons[i].Radius:Vector2.Distance(moons[i].Center,moons[i-6].Center)-moons[i].Radius-moons[i-6].Radius;
                Assert.That(gap,Is.InRange(6.499f,8.001f));
            }
            Assert.Greater(outer,65);Assert.Greater(largest-smallest,1);
        }
        Assert.AreNotEqual(DemoGalaxy.Layout(1)[0].Center,DemoGalaxy.Layout(2)[0].Center);
        Assert.AreEqual(DemoGalaxy.Layout(3)[8].Center,DemoGalaxy.Layout(3)[8].Center);
    }
    [Test] public void MemoryAndCipherRejectWrongAnswersAndRequireAllSixNotes()
    {
        foreach(int index in new[]{0,3})
        {
            var c=new DemoChallenge();c.Begin(index,101);
            if(index==0){Assert.IsFalse(c.Note(c.Sequence[0]));c.Tick(DemoChallenge.PreviewDuration);}
            c.Note((c.Sequence[0]+1)%7);Assert.AreEqual(1,c.Strikes);Assert.AreEqual(0,c.Step);
            c.Note((c.Sequence[0]+1)%7);Assert.IsTrue(c.Finished);Assert.IsFalse(c.Success);
            c.Begin(index,101);if(index==0)c.Tick(DemoChallenge.PreviewDuration);
            for(int i=0;i<5;i++)Assert.IsTrue(c.Note(c.Sequence[i]));Assert.IsFalse(c.Finished);
            c.Note(c.Sequence[5]);Assert.IsTrue(c.Success);c.Note(0);Assert.AreEqual(6,c.Step);
        }
    }
    [Test] public void PulseRequiresTenAccurateNotesAndPenalizesMashing()
    {
        var c=new DemoChallenge();c.Begin(1,2);c.Note(0);c.Note(1);c.Note(2);Assert.IsTrue(c.Finished);Assert.IsFalse(c.Success);
        c.Begin(1,2);
        for(int i=0;i<12;i++){c.Tick(DemoChallenge.BeatTime(i)-c.Elapsed);if(i<10)Assert.IsTrue(c.Note(c.Sequence[i]));}
        c.Tick(1);Assert.IsTrue(c.Success);Assert.AreEqual(2,c.Strikes);
        c.Begin(1,2);c.Tick(DemoChallenge.BeatTime(0)+.121f);Assert.IsFalse(c.Note(c.Sequence[0]));Assert.AreEqual(0,c.Step);
    }
    [Test] public void MovingStarsRequireEightHitsAndFailOnMissesOrTimeout()
    {
        var c=new DemoChallenge();c.Begin(2,3);Vector2 initial=c.TargetPosition;c.Tick(.5f);Assert.AreNotEqual(initial,c.TargetPosition);
        for(int i=0;i<3;i++)c.Shoot(Vector2.one*4);Assert.IsTrue(c.Finished);Assert.IsFalse(c.Success);
        c.Begin(2,3);for(int i=0;i<8;i++){Assert.IsTrue(c.Shoot(c.TargetPosition));c.Tick(.2f);}Assert.IsTrue(c.Success);
        c.Begin(2,3);c.Tick(16.1f);Assert.IsFalse(c.Success);Assert.IsFalse(c.Shoot(c.TargetPosition));
    }
    [Test] public void MazeWallsPreventTunnelingAndThreeCollisionsFail()
    {
        var c=new DemoChallenge();c.Begin(4,4);
        for(int i=0;i<3;i++){c.Move(Vector2.right,3);c.Tick(.6f);}
        Assert.IsTrue(c.Finished);Assert.IsFalse(c.Success);Assert.AreEqual(new Vector2(-.88f,-.75f),c.MazePosition);
        foreach(int i in new[]{0,1,2,3,4}){c.Begin(i,4);c.Tick(40);Assert.IsTrue(c.Finished);Assert.IsFalse(c.Success);}
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
    DemoGame g; Keyboard keyboard; Mouse mouse;
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
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();g.StartGame();yield return new WaitForSeconds(.2f);
    }
    [UnityTearDown] public IEnumerator TearDown()
    {
        Time.timeScale=1;InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);
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
    [UnityTest] public IEnumerator LeftShootsWithoutMovementAndRightSharesSpaceBoostBudget()
    {
        float peak=0;yield return Press(Key.Space);yield return Press();
        while(!g.Player.IsGrounded){peak=Mathf.Max(peak,g.Planets.respawnPlanet.SurfaceDistance(g.Player.Body.position)-.3f);yield return null;}
        Assert.That(peak,Is.InRange(1.1f,1.8f));
        int boosts=g.Player.FluteJumpCount;Assert.IsFalse(g.RecoilToward(g.Player.Body.position+Vector2.right*5));
        yield return Press(Key.Space);yield return Press();
        Vector2 before=g.Player.Body.linearVelocity;int shots=g.Shots;
        g.ShootToward(g.Player.Body.position+Vector2.right*5);
        Assert.AreEqual(before,g.Player.Body.linearVelocity);Assert.AreEqual(boosts,g.Player.FluteJumpCount);Assert.AreEqual(shots+1,g.Shots);
        Assert.IsTrue(g.RecoilToward(g.Player.Body.position+Vector2.right*5), "before recoil ground="+g.Player.IsGrounded+" used="+g.Player.FluteUsed+" normal="+g.Player.NormalJumpCount+" boosts="+g.Player.FluteJumpCount+" v="+g.Player.Body.linearVelocity);
        Assert.Less(g.Player.Body.linearVelocity.x,-4.5f);Assert.IsFalse(g.Player.CanFluteJump);Assert.AreEqual(boosts+1,g.Player.FluteJumpCount);
        Assert.IsFalse(g.RecoilToward(g.Player.Body.position+Vector2.left*5));
        yield return new WaitForSeconds(.25f);before=g.Player.Body.linearVelocity;shots=g.Shots;
        g.ShootToward(g.Player.Body.position+Vector2.left*5);Assert.AreEqual(before,g.Player.Body.linearVelocity);Assert.AreEqual(shots+1,g.Shots);
        yield return Press(Key.Space);yield return Press();Assert.AreEqual(boosts+1,g.Player.FluteJumpCount);
        g.Player.Respawn(g.Planets.respawnPlanet,Vector2.up);yield return Land();
        yield return Press(Key.Space);yield return Press();yield return Press(Key.Space);yield return Press();
        Assert.IsFalse(g.RecoilToward(g.Player.Body.position+Vector2.down*5));
    }
    [UnityTest] public IEnumerator LeftAndRightMouseBindingsRemainSeparateInActualInput()
    {
        yield return Press(Key.Space);yield return Press();
        Vector2 screen=Camera.main.WorldToScreenPoint(g.Player.Body.position+Vector2.right*5);
        int shots=g.Shots,boosts=g.Player.FluteJumpCount;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=screen}.WithButton(MouseButton.Left));yield return null;yield return null;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=screen});yield return null;
        Assert.AreEqual(shots+1,g.Shots);Assert.AreEqual(boosts,g.Player.FluteJumpCount);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=screen}.WithButton(MouseButton.Right));yield return null;yield return null;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=screen});yield return null;
        Assert.AreEqual(boosts+1,g.Player.FluteJumpCount);Assert.AreEqual(shots+1,g.Shots);
    }
    [UnityTest] public IEnumerator FullCircleWalkingStaysAttachedOnSmallLargeAndHomeWorlds()
    {
        var bodies=g.Player.gravityManager.bodies;var small=bodies[1];var large=bodies[1];
        foreach(var b in bodies){if(b.radius<small.radius)small=b;if(b!=bodies[0] && b.radius>large.radius)large=b;}
        foreach(var body in new[]{small,large,bodies[0]})
        {
            g.Player.Respawn(body,Vector2.up);yield return Land();int recoveries=g.Planets.RespawnCount;
            float end=Time.time+2*Mathf.PI*(body.radius+.31f)/g.Player.moveSpeed+.5f;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));
            while(Time.time<end)
            {
                yield return new WaitForFixedUpdate();
                Assert.IsTrue(g.Player.IsGrounded,"Lost attachment on "+body.planetId);
                Assert.That(body.SurfaceDistance(g.Player.Body.position),Is.InRange(.279f,.36f));
                Assert.AreEqual(body,g.Player.gravityManager.CurrentBody);
            }
            yield return Press();Assert.AreEqual(recoveries,g.Planets.RespawnCount);
        }
    }
    [UnityTest] public IEnumerator MinimapFollowsPositionAndKeepsNorthFixed()
    {
        var target=g.Player.gravityManager.bodies[5];
        foreach(var up in new[]{Vector2.up,Vector2.left,Vector2.down})
        {
            g.Player.Respawn(target,up);yield return Land();yield return null;
            Assert.AreEqual(Vector2.zero,g.Hud.MinimapPlayerPosition);
            Assert.That(Vector2.Distance(DemoHud.MinimapOffset(target.Center,g.Player.Body.position),g.Hud.MinimapBodyPosition(5)),Is.LessThan(.01f));
            Vector2 north=DemoHud.MinimapOffset(g.Player.Body.position+Vector2.up*5,g.Player.Body.position);
            Assert.AreEqual(0,north.x);Assert.Greater(north.y,0);
        }
    }
    [UnityTest] public IEnumerator InwardAndSidewaysRecoilCanLandBeforeClearingLaunchZone()
    {
        var home=g.Planets.respawnPlanet;
        foreach(var aim in new[]{Vector2.up,Vector2.right})
        {
            g.Player.Respawn(home,Vector2.up);yield return Land();yield return new WaitForSeconds(.25f);
            int recoveries=g.Planets.RespawnCount;
            yield return Press(Key.Space);yield return Press();
            g.RecoilToward(g.Player.Body.position+aim*10);
            Assert.IsTrue(g.Player.FluteUsed);yield return Land(4);
            Assert.AreEqual(home,g.Planets.CurrentPlanet);Assert.AreEqual(recoveries,g.Planets.RespawnCount);
            Assert.IsFalse(g.Player.FluteUsed);yield return Press(Key.Space);yield return Press();Assert.IsTrue(g.Player.CanFluteJump,"Rejump ground="+g.Player.IsGrounded+" used="+g.Player.FluteUsed+" normal="+g.Player.NormalJumpCount+" boosts="+g.Player.FluteJumpCount);
        }
    }
    [UnityTest] public IEnumerator EveryGeneratedMoonHasAWorkingRoundTripWithTheStarterFlute()
    {
        var bodies=g.Player.gravityManager.bodies;Assert.AreEqual(25,bodies.Length);
        for(int i=1;i<bodies.Length;i++)
        {
            var parent=i<=6?bodies[0]:bodies[i-6];
            for(int leg=0;leg<2;leg++)
            {
                var origin=leg==0?parent:bodies[i];var target=leg==0?bodies[i]:parent;
                Vector2 direction=(target.Center-origin.Center).normalized;
                g.Player.Respawn(origin,direction);yield return Land();
                int recoveries=g.Planets.RespawnCount;
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
                Assert.IsTrue(g.Player.IsGrounded,"Route "+i+" leg "+leg+" seed="+g.Seed+" pos="+g.Player.Body.position+" target="+target.Center);
                Assert.AreEqual(target,g.Planets.CurrentPlanet,"Moon "+i+" route leg "+leg);
                Assert.AreEqual(recoveries,g.Planets.RespawnCount,"Travel must not rely on recovery");
            }
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
        Assert.AreEqual(5,q.Stations.Length);Assert.AreEqual(5,new System.Collections.Generic.HashSet<GravityBody>(q.FragmentBodies).Count);
        Assert.IsFalse(g.Collect(q,0));
        for(int index=4;index>=0;index--)
        {
            var station=q.Stations[index];g.Player.Respawn(station.Body,(Vector2)station.transform.position-station.Body.Center);yield return Land();
            Assert.IsFalse(q.Fragments[index],"Touching alone must not award page");
            g.Interact();Assert.AreEqual(DemoState.Dialogue,g.State);g.AdvanceDialogue();g.AdvanceDialogue();Assert.AreEqual(DemoState.Challenge,g.State);
            if(index==4)
            {
                g.Challenge.Tick(23);Assert.IsFalse(g.Challenge.Success);g.FinishChallenge();Assert.IsFalse(q.Fragments[index]);
                Assert.IsTrue(g.BeginChallenge(index));g.Pause();float elapsed=g.Challenge.Elapsed;yield return new WaitForSecondsRealtime(.1f);Assert.AreEqual(elapsed,g.Challenge.Elapsed);g.Resume();
            }
            yield return DemoSmoke.SolveChallenge(g,keyboard,mouse);
            Assert.IsTrue(g.Challenge.Success,"Challenge "+index+" failed elapsed="+g.Challenge.Elapsed+" pos="+g.Challenge.MazePosition+" strikes="+g.Challenge.Strikes+" step="+g.Challenge.Step);if(index==3)g.LeaveChallenge();else g.FinishChallenge();Assert.IsTrue(q.Fragments[index]);Assert.IsFalse(g.Collect(q,index));
        }
        Assert.AreEqual(5,q.Count);
        Assert.IsFalse(g.BeginRhythm(q),"No performance away from home");Assert.AreEqual(0,g.Completed);
        g.Player.Respawn(g.Planets.respawnPlanet,Vector2.up);yield return Land();
        g.Interact();g.CloseDialogue();Assert.IsFalse(g.MelodyRepaired,"Cancel cannot repair score");
        g.Interact();g.AdvanceDialogue();g.AdvanceDialogue();Assert.IsTrue(g.MelodyRepaired);Assert.AreEqual(DemoState.Dialogue,g.State);
        g.AdvanceDialogue();g.AdvanceDialogue();Assert.AreEqual(DemoState.Rhythm,g.State);
        g.Pause();yield return null;g.Resume();Assert.AreEqual(0,g.Rhythm.Hits);
        while(g.State==DemoState.Rhythm)yield return null;
        Assert.IsFalse(g.LastPassed);Assert.AreEqual(0,g.Completed);Assert.AreEqual(5,q.Count);
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
