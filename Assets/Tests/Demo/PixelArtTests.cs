#if UNITY_EDITOR
using System.Collections;
using System.IO;
using FlyMeToTheMoon;
using FlyMeToTheMoon.Demo;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PixelArtTests
{
    DemoGame game;
    [UnitySetUp] public IEnumerator SetUp()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/FullDemo.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null; yield return null;
        game = Object.FindAnyObjectByType<DemoGame>(); game.StartGame();
        yield return new WaitForSeconds(.3f);
    }
    [UnityTearDown] public IEnumerator TearDown() { Time.timeScale = 1; yield return null; }
    [UnityTest] public IEnumerator AllArtRegionsLoadWithoutChangingWorldCollisionGeometry()
    {
        Assert.AreEqual(215, PixelArtLibrary.Regions.Count);
        foreach (var pair in PixelArtLibrary.Regions)
        {
            var sprite = PixelArtLibrary.Get(pair.Key);
            Assert.NotNull(sprite); Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode);
            Assert.AreEqual(1, sprite.texture.mipmapCount, pair.Key);
            Assert.LessOrEqual(sprite.rect.xMax, sprite.texture.width);
            Assert.LessOrEqual(sprite.rect.yMax, sprite.texture.height);
            var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Resources/PixelArt/" + pair.Value.sheet + ".png");
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
        }
        Assert.AreEqual(25, game.Art.Worlds.Count);
        var bodies = game.Player.gravityManager.bodies;
        for (int i = 0; i < bodies.Length; i++)
        {
            Assert.That(bodies[i].GetComponent<CircleCollider2D>().radius, Is.EqualTo(bodies[i].radius).Within(.001f));
            Assert.That(game.Art.Worlds[i].bounds.size.x, Is.EqualTo(bodies[i].radius * 2).Within(.01f));
            Assert.AreEqual(0, game.Art.Worlds[i].GetComponents<Collider2D>().Length);
            Assert.IsTrue(game.Art.Worlds[i].sharedMaterial.shader.isSupported);
        }
        Assert.AreEqual(5, game.Melody.Stations.Length);
        yield return Capture("home");
        game.ToggleMap(); yield return Capture("map"); game.ToggleMap();
    }
    [UnityTest] public IEnumerator TravelerFollowsLocalGravityAndShootingOnlyAnimatesEquipment()
    {
        var player = game.Player; var art = game.Art.PlayerArt;
        foreach (var up in new[] { Vector2.right, Vector2.down, Vector2.left, Vector2.up })
        {
            player.Respawn(game.Planets.respawnPlanet, up); yield return new WaitForSeconds(.15f);
            Assert.Greater(Vector2.Dot(art.Traveler.transform.up, up), .99f);
            Assert.That(art.Traveler.transform.localPosition.y, Is.EqualTo(-.29f).Within(.001f));
        }
        var velocity = player.Body.linearVelocity;
        int jumps = player.FluteJumpCount;
        game.ShootToward(player.Body.position + player.Up * 5);
        Assert.AreEqual(velocity, player.Body.linearVelocity); Assert.AreEqual(jumps, player.FluteJumpCount);
        yield return null; yield return null;
        Assert.IsTrue(art.Instrument.enabled);
        Assert.AreEqual("09/actor/1", art.Pose);
        foreach (var mesh in player.GetComponentsInChildren<MeshRenderer>()) Assert.IsFalse(mesh.enabled);
        game.Pause(); var pose = art.Pose; var position = art.Traveler.transform.position;
        yield return new WaitForSecondsRealtime(.3f);
        Assert.AreEqual(pose, art.Pose); Assert.AreEqual(position, art.Traveler.transform.position);
        game.Resume(); yield return new WaitForSeconds(.4f); Assert.IsFalse(art.Instrument.enabled);
    }
    [UnityTest] public IEnumerator EncounterArtTracksBellGateSwitchAndCreatureStates()
    {
        for (int index = 0; index < 5; index++)
        {
            var station = game.Melody.Stations[index];
            game.Player.Respawn(station.Body, (Vector2)station.transform.position - station.Body.Center);
            yield return new WaitForSeconds(.15f);
            Assert.IsTrue(game.BeginChallenge(index)); yield return null;
            Assert.Greater(game.Challenge.Root.GetComponentsInChildren<DemoEncounterArt>(true).Length, 0);
            if (index == 1)
            {
                var giant = (GiantEncounter)game.Challenge.Encounter;
                var bell = giant.Bells[0].GetComponent<DemoEncounterArt>();
                giant.Bells[0].Hit(false); yield return new WaitForSeconds(.1f);
                Assert.Greater(giant.LureRemaining, 0); Assert.AreNotEqual("01/object/0", bell.Sprite.sprite.name);
            }
            if (index == 2)
            {
                var storm = (StormEncounter)game.Challenge.Encounter;
                game.enabled = false; storm.Tick(6, false); storm.Switches[0].Hit(false); yield return null;
                Assert.AreEqual(DemoGame.Accent, storm.Switches[0].GetComponent<DemoEncounterArt>().Sprite.color);
                game.enabled = true;
            }
            if (index == 3)
            {
                var shepherd = (ShepherdEncounter)game.Challenge.Encounter;
                shepherd.Door.SetOpen(true); yield return new WaitForSeconds(.2f);
                Assert.IsFalse(shepherd.Door.Collider.enabled);
                Assert.Greater(Quaternion.Angle(Quaternion.identity, shepherd.Door.Shape.GetComponent<DemoEncounterArt>().Sprite.transform.localRotation), 70);
            }
            yield return Capture("encounter-" + index);
            game.LeaveChallenge(); yield return null;
        }
        LogAssert.NoUnexpectedReceived();
    }
    static IEnumerator Capture(string name)
    {
        if (Application.isBatchMode) yield break; // Batch tests validate state; render captures require an Editor Game view.
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../tmp/art-validation"));
        Directory.CreateDirectory(directory);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, name + ".png"));
        for (int i = 0; i < 5; i++) yield return null;
    }
}
#endif
