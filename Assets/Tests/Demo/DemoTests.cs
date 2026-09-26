#if UNITY_EDITOR
using System.Collections;
using FlyMeToTheMoon.Demo;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public class DemoTests
{
    [UnityTest] public IEnumerator ArtMakesHomeRicherThanEveryMoonWithoutChangingWorldPhysics()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/FullDemo.unity",new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;yield return null;
        var g=Object.FindAnyObjectByType<DemoGame>();var art=Object.FindAnyObjectByType<DemoArt>();
        Assert.NotNull(g);Assert.NotNull(art);
        Assert.Greater(art.Count(DemoArtRegion.HomeLandmark),20);
        Assert.Greater(art.Count(DemoArtRegion.HomeTerrain),40);
        for(int i=0;i<g.Quests.Count;i++)
        {
            Assert.Greater(art.CountMoon(i),4,"Each moon keeps a readable themed landmark.");
            Assert.Less(art.CountMoon(i),art.Count(DemoArtRegion.HomeLandmark)+art.Count(DemoArtRegion.HomeTerrain),
                "612-B must remain more visually complex than a moon.");
        }
        Assert.AreEqual(6,g.Player.gravityManager.bodies.Length);
        Assert.AreEqual(12f,g.Planets.respawnPlanet.radius,0.001f);
    }

    [Test] public void TimingWindowsWrongLanesAndDuplicateHits()
    {
        var r=new DemoRhythm();r.Begin(0,0);
        Assert.AreEqual("Keep the beat",r.Hit(1,2));
        Assert.AreEqual("PERFECT",r.Hit(0,2.1));
        r.Hit(0,2);Assert.AreEqual(1,r.Hits);
        Assert.AreEqual("GOOD",r.Hit(r.Lane(1),r.TimeOf(1)+.2));
        r.Hit(r.Lane(2),r.TimeOf(2)+.201);Assert.AreEqual(2,r.Hits);
        Assert.IsTrue(r.Tick(r.Duration));Assert.AreEqual(22,r.Misses);Assert.IsFalse(r.Passed);
        r.Begin(8,1);Assert.AreEqual(0,r.Hits);Assert.AreEqual(0,r.Misses);Assert.IsTrue(r.Running);
    }
    [Test] public void PassingRequiresSeventeenOfTwentyFourNotes()
    {
        var r=new DemoRhythm();r.Begin(0,0);
        for(int i=0;i<16;i++)r.Hit(r.Lane(i),r.TimeOf(i));
        Assert.IsFalse(r.Passed);r.Hit(r.Lane(16),r.TimeOf(16));Assert.IsTrue(r.Passed);
    }
    [UnityTest] public IEnumerator FullFiveMoonProgressionWithProjectilesPauseRetryAndEnding()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/FullDemo.unity",new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;yield return null;
        var g=Object.FindAnyObjectByType<DemoGame>();Assert.NotNull(g);Assert.AreEqual(DemoState.Title,g.State);
        g.StartGame();yield return new WaitForSeconds(.2f);
        Assert.AreEqual(5,g.Quests.Count);Assert.IsFalse(g.Collect(g.Quests[1],0));
        Assert.IsFalse(g.BeginRhythm(g.ActiveQuest));
        g.Pause();Assert.AreEqual(0,Time.timeScale);Assert.IsFalse(g.Player.Body.simulated);
        g.Resume();Assert.AreEqual(1,Time.timeScale);
        foreach(var q in g.Quests)
        {
            g.Player.Respawn(q.Body,(Vector2)q.Walk.transform.position-q.Body.Center);
            yield return new WaitForSeconds(.25f);Assert.IsTrue(q.Fragments[0],"Walk collection");
            g.Player.Respawn(q.Body,(Vector2)q.Shot.transform.position-q.Body.Center);
            yield return new WaitForSeconds(.25f);g.ShootToward(q.Shot.transform.position);
            yield return new WaitForSeconds(.25f);Assert.IsTrue(q.Fragments[1],"Shot collection");
            Assert.IsFalse(g.Collect(q,1));Assert.IsFalse(g.Collect(q,2));
            g.HitTarget(q.Resonators[1]);g.HitTarget(q.Resonators[2]);Assert.AreEqual(0,q.SequenceStep);
            g.HitTarget(q.Resonators[1]);g.HitTarget(q.Resonators[0]);g.HitTarget(q.Resonators[2]);Assert.IsTrue(q.PuzzleSolved);
            g.Player.Respawn(q.Body,q.Pedestal-q.Body.Center);
            yield return new WaitForSeconds(.25f);Assert.AreEqual(3,q.Count);
            Assert.IsTrue(g.BeginRhythm(q));Assert.IsFalse(g.Player.Body.simulated);
            if(q.Index==0)
            {
                g.Pause();yield return null;g.Resume();Assert.AreEqual(0,g.Rhythm.Hits);
                while(g.State==DemoState.Rhythm)yield return null;
                Assert.IsFalse(g.LastPassed);Assert.AreEqual(0,g.Completed);Assert.AreEqual(3,q.Count);
                Assert.IsTrue(g.BeginRhythm(q));
            }
            for(int i=0;i<DemoRhythm.NoteCount;i++)
            {
                while(g.SongTime<g.Rhythm.TimeOf(i))yield return null;
                g.PlayLane(g.Rhythm.Lane(i));
            }
            while(g.State==DemoState.Rhythm)yield return null;
            Assert.IsTrue(g.LastPassed);Assert.AreEqual(24,g.Rhythm.Hits);Assert.AreEqual(q.Index+1,g.Completed);
            Assert.IsTrue(q.Unlocked);Assert.AreEqual(g.Completed,g.Equipped);
            if(q.Index==4)Assert.AreEqual(DemoState.Win,g.State);
            g.Continue();Assert.IsTrue(g.Player.Body.simulated);
        }
        Assert.IsNull(g.ActiveQuest);g.CycleInstrument();Assert.AreEqual(0,g.Equipped);
        g.ToggleMap();Assert.IsTrue(g.MapVisible);g.ToggleAudio();Assert.IsTrue(g.Audio.Muted);
    }
}
#endif
