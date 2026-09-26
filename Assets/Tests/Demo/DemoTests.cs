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
    [Test] public void ThousandSeedsHaveIrregularConnectedMoonsAndBendingRelayRoutes()
    {
        var rootCounts=new System.Collections.Generic.HashSet<int>();
        for(int seed=1;seed<=1000;seed++)
        {
            var moons=DemoGalaxy.Layout(seed);Assert.AreEqual(24,moons.Length);
            float outer=0,smallest=10,largest=0;int roots=0;
            Vector2 minimum=Vector2.zero,maximum=Vector2.zero;
            for(int i=0;i<moons.Length;i++)
            {
                var moon=moons[i];Assert.That(moon.Radius,Is.InRange(3.2f,5.8f));
                Assert.That(moon.ParentIndex,Is.InRange(-1,i-1));
                Vector2 parent=moon.ParentIndex<0?Vector2.zero:moons[moon.ParentIndex].Center;
                float parentRadius=moon.ParentIndex<0?12:moons[moon.ParentIndex].Radius;
                Assert.AreEqual(moon.ParentIndex<0?1:moons[moon.ParentIndex].Depth+1,moon.Depth);
                if(moon.ParentIndex<0)roots++;
                else
                {
                    var ancestor=moons[moon.ParentIndex].ParentIndex;
                    Vector2 incoming=parent-(ancestor<0?Vector2.zero:moons[ancestor].Center);
                    Assert.GreaterOrEqual(Vector2.Angle(incoming,moon.Center-parent),24.99f,"No straight radial strings");
                }
                smallest=Mathf.Min(smallest,moon.Radius);largest=Mathf.Max(largest,moon.Radius);
                outer=Mathf.Max(outer,moon.Center.magnitude+moon.Radius);
                minimum=Vector2.Min(minimum,moon.Center);maximum=Vector2.Max(maximum,moon.Center);
                Assert.GreaterOrEqual(moon.Center.magnitude-12-moon.Radius,6.499f);
                for(int j=0;j<i;j++)Assert.GreaterOrEqual(Vector2.Distance(moon.Center,moons[j].Center)-moon.Radius-moons[j].Radius,6.499f);
                Assert.That(Vector2.Distance(moon.Center,parent)-moon.Radius-parentRadius,Is.InRange(6.499f,8.001f));
            }
            rootCounts.Add(roots);Assert.GreaterOrEqual(outer,70);Assert.GreaterOrEqual(maximum.x-minimum.x,85);Assert.GreaterOrEqual(maximum.y-minimum.y,85);
            Assert.Greater(largest-smallest,1);
            var route=DemoGalaxy.RelayPath(moons,seed);Assert.AreEqual(4,route.Length);
            Assert.AreEqual(4,new System.Collections.Generic.HashSet<int>(route).Count);
            Assert.AreEqual(-1,moons[route[0]].ParentIndex);
            for(int i=1;i<route.Length;i++)Assert.AreEqual(route[i-1],moons[route[i]].ParentIndex);
            CollectionAssert.AreEqual(route,DemoGalaxy.RelayPath(moons,seed));
        }
        Assert.GreaterOrEqual(rootCounts.Count,3,"The home planet must not have a fixed set of spokes");
        Assert.AreNotEqual(DemoGalaxy.Layout(1)[0].Center,DemoGalaxy.Layout(2)[0].Center);
        Assert.AreEqual(DemoGalaxy.Layout(3)[8].Center,DemoGalaxy.Layout(3)[8].Center);
    }
    [Test] public void SevenNotesCoverAMajorScale()
    {
        Assert.That(DemoAudio.FrequencyForNote(0),Is.EqualTo(261.6256f).Within(.01));
        Assert.That(DemoAudio.FrequencyForNote(6),Is.EqualTo(493.8833f).Within(.01));
        for(int i=1;i<7;i++)Assert.Greater(DemoAudio.FrequencyForNote(i),DemoAudio.FrequencyForNote(i-1));
    }
    [Test] public void CreaturePlanIsDeterministicUniqueAndSpreadsRolesAcrossTravelDepths()
    {
        for(int seed=1;seed<=1000;seed++)
        {
            var layout=DemoGalaxy.Layout(seed);int[] plan=DemoWorldEvents.Plan(seed,layout);Assert.AreEqual(DemoWorldEvents.CreatureCount,plan.Length);
            var order=new System.Collections.Generic.List<int>();for(int i=0;i<layout.Length;i++)order.Add(i);
            order.Sort((a,b)=>layout[a].Depth!=layout[b].Depth?layout[a].Depth.CompareTo(layout[b].Depth):a.CompareTo(b));
            var unique=new System.Collections.Generic.HashSet<int>(plan);Assert.AreEqual(plan.Length,unique.Count);
            for(int i=0;i<plan.Length;i++)
            {
                Assert.That(plan[i],Is.InRange(0,DemoGalaxy.MoonCount-1));
                Assert.AreEqual(i/2,order.IndexOf(plan[i])/(DemoGalaxy.MoonCount/4));
            }
            CollectionAssert.AreEqual(plan,DemoWorldEvents.Plan(seed,layout));
        }
        CollectionAssert.AreNotEqual(DemoWorldEvents.Plan(1),DemoWorldEvents.Plan(2));
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
            var parent=bodies[g.MoonLayout[i-1].ParentIndex+1];
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
            g.Interact();Assert.AreEqual(DemoState.Dialogue,g.State);DemoSmoke.AdvanceAll(g);Assert.AreEqual(DemoState.Challenge,g.State);
            Assert.IsTrue(g.Player.enabled);Assert.IsTrue(g.Player.Body.simulated,"Encounters use actual world movement");
            if(index==4)
            {
                g.Pause();float elapsed=g.Challenge.Elapsed;yield return new WaitForSecondsRealtime(.1f);Assert.AreEqual(elapsed,g.Challenge.Elapsed);g.Resume();
            }
            var encounter=g.Challenge.Encounter;
            yield return DemoSmoke.SolveChallenge(g,keyboard,mouse);
            Assert.IsTrue(g.Challenge.Success,"Encounter "+index+" failed: "+encounter.Status+" pos="+encounter.Along(g.Player.Body.position)+" height="+encounter.Body.SurfaceDistance(g.Player.Body.position));
            Assert.IsTrue(q.Fragments[index]);Assert.IsFalse(g.Collect(q,index));
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
    IEnumerator BeginEncounter(int index)
    {
        var station=g.Melody.Stations[index];
        g.Player.Respawn(station.Body,(Vector2)station.transform.position-station.Body.Center);yield return Land();
        g.Interact();DemoSmoke.AdvanceAll(g);yield return new WaitForSeconds(.2f);
        Assert.AreEqual(DemoState.Challenge,g.State);
    }
    IEnumerator StandAt(WorldEncounter encounter,float distance)
    {
        g.Player.Respawn(encounter.Body,encounter.At(distance)-encounter.Body.Center);yield return Land();
    }
    [UnityTest] public IEnumerator EchoClosedGateBlocksMovementAndRerecordClearsPastActions()
    {
        yield return BeginEncounter(4);var echo=(EchoEncounter)g.Challenge.Encounter;
        Assert.IsFalse(g.Melody.Stations[4].gameObject.activeSelf,"Hide the beacon during its encounter");
        echo.ReceiverTarget.Hit(false);Assert.AreEqual(0,echo.Stage);Assert.IsFalse(echo.Complete);
        yield return StandAt(echo,4.7f);
        yield return Press(Key.D);yield return new WaitForSeconds(.8f);yield return Press();
        Assert.Less(echo.Along(g.Player.Body.position),5.7f,"Closed gate must stop the real controller");
        yield return Press(Key.C);yield return Press();
        yield return DemoSmoke.Walk(g,echo,keyboard,2.8f);
        yield return DemoSmoke.Fire(g,mouse,g.Player.Body.position+g.Player.Up*5);
        Assert.IsTrue(echo.Recording);Assert.Greater(echo.Frames.Count,2);Assert.AreEqual(1,echo.Shots.Count);
        yield return Press(Key.E);yield return Press();
        Assert.IsTrue(echo.Playing);Assert.IsFalse(echo.Recording);
        yield return Press(Key.C);yield return Press();
        Assert.IsTrue(echo.Recording);Assert.IsFalse(echo.Playing);Assert.AreEqual(0,echo.Shots.Count);
        Assert.Less(echo.Clock,.2f);Assert.IsFalse(echo.GhostOnPlate);
        var root=g.Challenge.Root;g.LeaveChallenge();yield return null;
        Assert.IsTrue(root==null);Assert.IsTrue(g.Melody.Stations[4].gameObject.activeSelf,"Leaving restores the beacon for another attempt");Assert.AreEqual(0,Object.FindObjectsByType<EncounterShotTarget>(FindObjectsSortMode.None).Length);
        Assert.AreEqual(0,g.Melody.Count);
    }
    [UnityTest] public IEnumerator ShepherdResponsesNoiseHazardsAndGateAreDistinct()
    {
        yield return BeginEncounter(3);var e=(ShepherdEncounter)g.Challenge.Encounter;
        g.enabled=false;
        e.Tick(.2f,true);Assert.Greater(e.Creatures[0].Position,-2.5f);Assert.IsFalse(e.Creatures[1].Awake);
        e.Creatures[0].Position=ShepherdEncounter.Sanctuary;e.Tick(0,false);Assert.AreEqual(1,e.Rescued);
        e.Creatures[1].Position=e.Creatures[2].Position=-2;
        e.Tick(.2f,true);Assert.AreEqual(-2,e.Creatures[1].Position);Assert.AreEqual(-2,e.Creatures[2].Position);
        e.Tick(.6f,true);Assert.Greater(e.Creatures[1].Position,-2);Assert.AreEqual(-2,e.Creatures[2].Position);
        e.Tick(.3f,false);Assert.Greater(e.Creatures[2].Position,-2);
        float position=e.Creatures[1].Position;e.Shot(e.At(position+.5f),Vector2.up);e.Tick(.1f,false);
        Assert.Greater(e.Creatures[1].Fear,0);Assert.Less(e.Creatures[1].Position,position,"Noise drives nearby singers away");
        e.Creatures[1].Fear=0;e.Creatures[1].Position=e.Creatures[1].Target=-7.5f;e.Tick(0,false);
        Assert.That(e.Creatures[1].Position,Is.EqualTo(-3.7f).Within(.001));Assert.AreEqual(1,e.Rescued);
        yield return StandAt(e,1.4f);e.Interact();Assert.IsTrue(e.Door.Open);
        e.Interact();Assert.IsFalse(e.Door.Open);
        Assert.IsFalse(e.Complete);g.enabled=true;g.LeaveChallenge();
    }
    [UnityTest] public IEnumerator StormRejectsCoveredSwitchesAndHitsResetOnlyCurrentCircuit()
    {
        yield return BeginEncounter(2);var e=(StormEncounter)g.Challenge.Encounter;g.enabled=false;
        e.Switches[0].Hit(false);Assert.IsFalse(e.Repaired[0]);
        e.Tick(6,false);Assert.IsTrue(e.Exposed);
        e.Switches[0].Hit(true);Assert.IsFalse(e.Repaired[0],"A ghost cannot perform a real repair");
        e.Switches[0].Hit(false);Assert.IsTrue(e.Repaired[0]);
        e.Tick(3,false);e.Tick(2.64f,false);
        Assert.AreEqual(1,e.HitsTaken);Assert.IsFalse(e.Repaired[0]);Assert.AreEqual(0,e.Stage);
        Assert.Greater(g.Player.Body.linearVelocity.magnitude,2,"A hit must knock the player back");
        e.Tick(6,false);e.Switches[0].Hit(false);e.Switches[1].Hit(false);Assert.AreEqual(1,e.Stage);
        yield return Land();e.Tick(2.65f,false);
        Assert.AreEqual(1,e.HitsTaken,"Shelter blocks a high wave");
        e.Reset();yield return StandAt(e,2);e.Tick(2.4f,false);
        Assert.AreEqual(2,e.HitsTaken);Assert.AreEqual(1,e.Stage,"Completed circuits survive a hit");
        Assert.IsFalse(e.Complete);g.enabled=true;g.LeaveChallenge();
    }
    [UnityTest] public IEnumerator GiantNoiseWakesAndRealBellShotDistracts()
    {
        yield return BeginEncounter(1);var e=(GiantEncounter)g.Challenge.Encounter;
        for(int i=0;i<4;i++)e.Shot(e.At(e.SleepingPosition),Vector2.up);
        e.Tick(0,false);Assert.AreEqual(1,e.Wakes);Assert.AreEqual(0,e.Awareness);
        yield return Land();yield return Press(Key.Space);yield return Press();
        Assert.Greater(e.Awareness,.1f,"A real jump produces noise");
        yield return StandAt(e,1.6f);
        yield return DemoSmoke.Fire(g,mouse,e.Bells[1].transform.position);
        Assert.Greater(e.LureRemaining,4);Assert.That(e.Attention,Is.EqualTo(2.7f).Within(.01f));
        Assert.IsFalse(e.Complete);Assert.AreEqual(0,g.Melody.Count);g.LeaveChallenge();
    }
    [UnityTest] public IEnumerator SeededCreaturesTeachWithMultiPageAndShortRepeatDialogue()
    {
        Assert.NotNull(g.Events);Assert.AreEqual(1+DemoWorldEvents.CreatureCount,g.Events.Encounters.Count);
        Assert.AreEqual("keeper",g.Events.Encounters[0].EncounterId);
        var creature=g.Events.Encounters[1];
        Assert.AreEqual(DemoTargetKind.Creature,creature.Target.Kind);Assert.NotNull(creature.Target.Body);
        Assert.Contains(creature.Target,g.Targets);

        g.Player.Respawn(creature.Target.Body,(Vector2)creature.transform.position-creature.Target.Body.Center);
        yield return Land();
        Vector2 before=g.Player.Body.position;
        g.Events.Speak(creature);
        Assert.AreEqual(DemoState.Dialogue,g.State);Assert.AreEqual(3,g.DialoguePageCount);
        Assert.AreEqual(0,g.DialoguePageIndex);Assert.AreEqual(0,Time.timeScale);
        Assert.IsFalse(g.Player.Body.simulated);
        for(int page=0;page<3;page++)
        {
            g.AdvanceDialogue();Assert.IsTrue(g.Hud.DialogueComplete);
            g.AdvanceDialogue();
            if(page<2)Assert.AreEqual(page+1,g.DialoguePageIndex);
        }
        Assert.AreEqual(DemoState.Explore,g.State);Assert.IsTrue(creature.Seen);
        Assert.AreEqual(1,g.Events.SeenCount);
        Assert.That(Vector2.Distance(before,g.Player.Body.position),Is.LessThan(.03f),"Dialogue must freeze meaningful player movement");

        g.Events.Speak(creature);Assert.AreEqual(1,g.DialoguePageCount);
        g.AdvanceDialogue();g.AdvanceDialogue();Assert.AreEqual(DemoState.Explore,g.State);
        Assert.AreEqual(1,g.Events.SeenCount,"Repeat conversations cannot count twice");
    }
}
#endif
