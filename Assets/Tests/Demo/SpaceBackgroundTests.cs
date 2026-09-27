#if UNITY_EDITOR
using System.Collections;
using System.IO;
using FlyMeToTheMoon.Demo;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SpaceBackgroundTests
{
    DemoGame game;
    Camera camera;
    Component pixel;
    [UnitySetUp] public IEnumerator Setup()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/FullDemo.unity",new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;yield return null;
        game=Object.FindAnyObjectByType<DemoGame>();game.StartGame();camera=Camera.main;
        foreach(var c in camera.GetComponents<Component>())if(c.GetType().Name=="PixelPerfectCamera")pixel=c;
        Assert.NotNull(pixel);yield return new WaitForSeconds(.3f);
    }
    [UnityTearDown] public IEnumerator Cleanup(){Time.timeScale=1;yield return null;}

    [UnityTest] public IEnumerator SpaceCoversGroundFlightOuterMoonsAndRotatedWideViews()
    {
        StringAssert.Contains("Universal",UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name);
        var sky=game.Art.Space;var renderer=sky.GetComponent<MeshRenderer>();
        Assert.IsTrue(renderer.sharedMaterial.shader.isSupported);
        Assert.AreEqual(0,sky.GetComponentsInChildren<Collider2D>().Length);
        Assert.AreEqual(25,game.Player.gravityManager.bodies.Length);
        foreach(var key in new[]{"_Nebula","_DeepStars","_MiddleStars","_NearStars"})
        {
            var texture=(Texture2D)renderer.sharedMaterial.GetTexture(key);
            Assert.AreEqual(FilterMode.Point,texture.filterMode);Assert.AreEqual(1,texture.mipmapCount);
        }
        foreach(var world in game.Art.Worlds)Assert.Less(renderer.sortingOrder,world.sortingOrder);
        yield return Capture("home-ground");
        game.Planets.cameraController.enabled=false;Object.FindAnyObjectByType<DemoZoom>().enabled=false;
        game.Player.enabled=false;game.Player.Body.simulated=false;
        var moon=game.Player.gravityManager.bodies[8];
        var poses=new[]{Vector2.Lerp(game.Planets.respawnPlanet.Center,moon.Center,.5f),moon.Center+Vector2.up*(moon.radius+.5f),new Vector2(150,-150)};
        for(int i=0;i<poses.Length;i++)
        {
            pixel.GetType().GetProperty("assetsPPU").SetValue(pixel,i==1?40:10);
            camera.transform.position=(Vector3)poses[i]+Vector3.back*10;
            camera.transform.rotation=Quaternion.Euler(0,0,i==2?90:0);
            camera.aspect=i==2?2.8f:16f/9;
            yield return new WaitForSeconds(.2f);
            foreach(var corner in new[]{Vector2.zero,Vector2.one,Vector2.right,Vector2.up})
            {
                var position=camera.ViewportToWorldPoint(new Vector3(corner.x,corner.y,20));
                Assert.IsTrue(renderer.bounds.Contains(position),"The sky must cover the frame after travel, zoom or rotation");
            }
            yield return Capture(i==0?"flight":i==1?"outer-moon":"wide-rotated-space");
        }
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest] public IEnumerator ShootingStarsPauseAndSkyResourcesAreReleased()
    {
        var sky=game.Art.Space;var material=sky.GetComponent<MeshRenderer>().sharedMaterial;
        var mesh=sky.GetComponent<MeshFilter>().sharedMesh;
        var textures=new Texture[4];int index=0;
        foreach(var key in new[]{"_Nebula","_DeepStars","_MiddleStars","_NearStars"})textures[index++]=material.GetTexture(key);
        float end=Time.time+10;
        while(material.GetVector("_Meteor").z<.3f && Time.time<end)yield return null;
        Assert.Greater(material.GetVector("_Meteor").z,.3f,"A shooting star should fade in, not remain invisible");
        yield return Capture("shooting-star");
        game.Pause();float age=material.GetFloat("_SkyTime");Vector4 meteor=material.GetVector("_Meteor");
        yield return new WaitForSecondsRealtime(.2f);
        Assert.AreEqual(age,material.GetFloat("_SkyTime"));Assert.AreEqual(meteor,material.GetVector("_Meteor"));
        game.Resume();yield return new WaitForSeconds(.2f);Assert.Greater(material.GetFloat("_SkyTime"),age);
        Object.Destroy(sky.gameObject);yield return null;yield return null;
        Assert.IsTrue(material==null);Assert.IsTrue(mesh==null);
        foreach(var texture in textures)Assert.IsTrue(texture==null,"Restart must release generated sky textures");
        LogAssert.NoUnexpectedReceived();
    }

    static IEnumerator Capture(string name)
    {
        if(Application.isBatchMode)yield break;
        yield return new WaitForEndOfFrame();
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../space-background-review"));
        Directory.CreateDirectory(folder);ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));
        yield return null;yield return null;
    }
}
#endif
