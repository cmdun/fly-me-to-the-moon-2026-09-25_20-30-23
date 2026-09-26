#if UNITY_EDITOR
using System.Collections;
using FlyMeToTheMoon;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class PrototypePlayModeTests
{
    private Keyboard keyboard;
    private PlayerController player;
    private PlanetManager planets;
    private GravityManager gravity;
    private InputSettings.BackgroundBehavior previousBackground;
    private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Batch Test Runner has no focused Game view. Only this test session ignores focus.
        previousBackground = InputSystem.settings.backgroundBehavior;
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/FirstPlayablePrototype.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        keyboard = InputSystem.AddDevice<Keyboard>();
        player = Object.FindFirstObjectByType<PlayerController>();
        planets = Object.FindFirstObjectByType<PlanetManager>();
        gravity = player.gravityManager;
        yield return WaitForGround();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        InputSystem.RemoveDevice(keyboard);
        InputSystem.settings.backgroundBehavior = previousBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        yield return null;
    }

    private IEnumerator Press(params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        yield return null;
        yield return new WaitForFixedUpdate();
    }

    private IEnumerator WaitForGround(float timeout = 4f)
    {
        float end = Time.time + timeout;
        while (!player.IsGrounded && Time.time < end) yield return null;
        Assert.That(player.IsGrounded, Is.True, "Player should land within timeout.");
    }

    private IEnumerator Launch()
    {
        yield return Press(Key.Space);
        yield return Press();
        float end = Time.time + 2f;
        while (!gravity.IsTraveling && Time.time < end) yield return null;
        Assert.That(gravity.IsTraveling, Is.True, "Normal jump must clear the home lock zone.");
    }

    [UnityTest]
    public IEnumerator SpawnAndMoveAroundSurfaceWithCenteredCamera()
    {
        Assert.That(planets.CurrentPlanet, Is.EqualTo(planets.respawnPlanet));
        Vector2 before = player.Body.position;
        yield return Press(Key.D);
        yield return new WaitForSeconds(1.2f);
        yield return Press();
        Assert.That(Vector2.Distance(before, player.Body.position), Is.GreaterThan(3f));
        Assert.That(player.IsGrounded, Is.True);
        Assert.That(planets.respawnPlanet.SurfaceDistance(player.Body.position), Is.InRange(0.2f, 0.4f));
        Assert.That(Vector2.Distance(Camera.main.transform.position, player.transform.position), Is.LessThan(0.15f));
        yield return Press(Key.A);
        yield return new WaitForSeconds(1.2f);
        yield return Press();
        Assert.That(Vector2.Distance(before, player.Body.position), Is.LessThan(1f));
    }

    [UnityTest]
    public IEnumerator SecondJumpRejectedWhileLockedThenOneFluteBurstDuringTravel()
    {
        yield return Press(Key.Space);
        yield return Press();
        yield return Press(Key.Space);
        Assert.That(gravity.IsTraveling, Is.False);
        Assert.That(player.NormalJumpCount, Is.EqualTo(1));
        Assert.That(player.FluteJumpCount, Is.Zero);
        yield return Press();
        float end = Time.time + 2f;
        while (!gravity.IsTraveling && Time.time < end) yield return null;
        Assert.That(gravity.IsTraveling, Is.True);
        yield return Press(Key.Space);
        Assert.That(player.FluteJumpCount, Is.EqualTo(1));
        Assert.That(player.noteBurst.ActiveNotes, Is.EqualTo(6));
        Assert.That(player.noteBurst.BurstCount, Is.EqualTo(1));
        yield return Press();
        yield return Press(Key.Space);
        Assert.That(player.FluteJumpCount, Is.EqualTo(1), "A third jump must be rejected.");
        yield return Press();
        yield return new WaitForSeconds(0.8f);
        Assert.That(player.noteBurst.ActiveNotes, Is.Zero);
    }

    [UnityTest]
    public IEnumerator HomeToMoonAndBackRestoresJumpsOnlyOnLanding()
    {
        yield return Launch();
        yield return Press(Key.Space);
        yield return Press();
        yield return WaitForGround();
        Assert.That(planets.CurrentPlanet, Is.Not.EqualTo(planets.respawnPlanet));
        Assert.That(planets.CurrentPlanet.planetId, Is.EqualTo("Moon"));
        Assert.That(player.FluteUsed, Is.False);
        yield return Launch();
        yield return Press(Key.Space);
        yield return Press();
        yield return WaitForGround();
        Assert.That(planets.CurrentPlanet, Is.EqualTo(planets.respawnPlanet));
        Assert.That(player.NormalJumpCount, Is.EqualTo(2));
        Assert.That(player.FluteJumpCount, Is.EqualTo(2));
        Assert.That(planets.RespawnCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator MissedTransferFallsIntoSpaceAndRespawns()
    {
        // Move to the far side through real input, then jump away from the route.
        yield return Press(Key.D);
        yield return new WaitForSeconds(4.2f);
        yield return Press();
        yield return Launch();
        yield return Press(Key.Space);
        yield return Press();
        float end = Time.time + 13f;
        while (planets.RespawnCount == 0 && Time.time < end) yield return null;
        Assert.That(planets.RespawnCount, Is.EqualTo(1));
        yield return WaitForGround();
        Assert.That(planets.CurrentPlanet, Is.EqualTo(planets.respawnPlanet));
        Assert.That(Vector2.Distance(player.Body.position, new Vector2(0f, 6.28f)), Is.LessThan(0.2f));
        Assert.That(player.FluteUsed, Is.False);
    }
}
#endif
