#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FlyMeToTheMoon;
using FlyMeToTheMoon.Demo;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class AmbientLifeTests
{
    DemoGame game;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/FullDemo.unity",new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;yield return null;game=Object.FindAnyObjectByType<DemoGame>();
        game.StartGame();yield return new WaitForSeconds(.3f);
    }
    [UnityTearDown] public IEnumerator Cleanup(){Time.timeScale=1;yield return null;}
    static string DetectPipeline()
    {
        var pipeline=GraphicsSettings.currentRenderPipeline;
        if(pipeline==null)return "BuiltIn";
        var name=pipeline.GetType().FullName;
        return name.Contains("Universal")?"URP":name.Contains("HighDefinition")?"HDRP":"Custom";
    }
    [UnityTest] public IEnumerator AllWorldsHaveClearGravityAlignedMusicalScenery()
    {
        Assert.AreEqual("URP",DetectPipeline());
        var life=game.Art.Ambient;var bodies=new HashSet<GravityBody>();int creatures=0,sculptures=0;
        Assert.That(life.Residents.Count,Is.InRange(150,800));
        foreach(var r in life.Residents)
        {
            bodies.Add(r.Body);creatures+=r.Kind=="creature"?1:0;sculptures+=r.Kind=="shrine"?1:0;
            Assert.That(Vector2.Dot(r.Mount.up,((Vector2)r.Mount.position-r.Body.Center).normalized),Is.GreaterThan(.999f));
            Assert.That(Vector2.Distance(r.Mount.position,r.Body.Center),Is.InRange(r.Body.radius-.025f,r.Body.radius+.16f));
            Assert.AreEqual(0,r.Mount.GetComponentsInChildren<Collider2D>().Length);
            Assert.AreEqual(FilterMode.Point,r.Art.sprite.texture.filterMode);
            Assert.AreEqual(1,r.Art.sprite.texture.mipmapCount);Assert.AreEqual(40,r.Art.sprite.pixelsPerUnit);
            Assert.IsTrue(r.Art.sharedMaterial.shader.isSupported);
            Assert.IsFalse(game.Discoveries.Reserved(r.Body,r.Mount.position,.7f));
            foreach(var target in game.Targets)if(target.Body==r.Body)
                Assert.Greater(Vector2.Distance(r.Mount.position,target.transform.position),1.25f);
        }
        Assert.AreEqual(25,bodies.Count);Assert.GreaterOrEqual(creatures,25);Assert.GreaterOrEqual(sculptures,18);
        Assert.AreEqual(5,game.Melody.Stations.Length);Assert.AreEqual(12,game.Discoveries.Sites.Count);
        LogAssert.NoUnexpectedReceived();yield return null;
    }
    [UnityTest] public IEnumerator MusicAndProjectilesWakeNearbyLifeAndPauseFreezesIt()
    {
        var life=game.Art.Ambient;var r=life.Residents.Find(x=>x.Kind=="creature");
        game.Player.Respawn(r.Body,(Vector2)r.Mount.position-r.Body.Center);game.Planets.cameraController.SnapToPlayer();
        yield return new WaitForSeconds(.3f);
        int far=life.Residents[life.Residents.Count-1].Responses;
        game.FreeNote(4);yield return null;Assert.Greater(r.Responses,0);Assert.IsTrue(r.Note.enabled);
        Assert.AreEqual(far,life.Residents[life.Residents.Count-1].Responses);
        Vector3 pos=r.Mount.position;game.Pause();yield return new WaitForSecondsRealtime(.2f);
        Assert.AreEqual(pos,r.Mount.position);game.Resume();yield return new WaitForSeconds(1.6f);
        Assert.IsFalse(r.Note.enabled);int before=r.Responses;
        Vector2 target=(Vector2)r.Mount.position+(Vector2)r.Mount.up*.4f;
        game.Art.NotePassed(target-(Vector2)r.Mount.right*.2f,target+(Vector2)r.Mount.right*.2f);
        yield return null;Assert.AreEqual(before+1,r.Responses);Assert.IsTrue(r.Note.enabled);
        var station=game.Melody.Stations[0];game.Player.Respawn(station.Body,(Vector2)station.transform.position-station.Body.Center);
        yield return new WaitForSeconds(.2f);Assert.IsTrue(game.BeginChallenge(0));yield return null;
        var relay=(RelayEncounter)game.Challenge.Encounter;
        foreach(var item in life.Residents)foreach(var body in relay.Route)
            if(item.Body==body)Assert.IsFalse(item.Mount.gameObject.activeInHierarchy);
        game.LeaveChallenge();yield return null;
        foreach(var item in life.Residents)Assert.IsTrue(item.Mount.gameObject.activeInHierarchy);
        LogAssert.NoUnexpectedReceived();
    }
    [UnityTest] public IEnumerator RenderHomeAndAllSixMoonThemes()
    {
        if(Application.isBatchMode)yield break;
        game.Planets.cameraController.enabled=false;Object.FindAnyObjectByType<DemoZoom>().enabled=false;
        var camera=Camera.main;
        Component pixel=null;foreach(var component in camera.GetComponents<Component>())if(component.GetType().Name=="PixelPerfectCamera")pixel=component;
        Assert.NotNull(pixel);
        for(int index=0;index<7;index++)
        {
            var body=game.Player.gravityManager.bodies[index];
            game.Player.Respawn(body,Vector2.up);camera.transform.position=new Vector3(body.Center.x,body.Center.y,-10);camera.transform.rotation=Quaternion.identity;
            pixel.GetType().GetProperty("assetsPPU").SetValue(pixel,index==0?5:12);
            game.FreeNote(index%7);yield return Capture(index==0?"home-overview":"moon-theme-"+index);
        }
        var nearby=game.Art.Ambient.Residents.Find(r=>r.Body==game.Planets.respawnPlanet && r.Kind=="shrine");
        game.Player.Respawn(nearby.Body,(Vector2)nearby.Mount.position-nearby.Body.Center);
        camera.transform.position=nearby.Mount.position+nearby.Mount.up*1.2f+Vector3.back*10;
        camera.transform.rotation=nearby.Mount.rotation;pixel.GetType().GetProperty("assetsPPU").SetValue(pixel,24);
        game.FreeNote(3);yield return Capture("home-music-closeup");
        LogAssert.NoUnexpectedReceived();
    }
    static IEnumerator Capture(string name)
    {
        yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();
        string folder="/Users/williamxi/Documents/ChatGPT/Hackathon/tmp/scenery-views";Directory.CreateDirectory(folder);
        ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return null;yield return null;
    }
}
#endif
