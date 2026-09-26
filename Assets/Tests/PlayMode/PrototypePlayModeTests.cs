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
        Assert.That(player.IsGrounded, Is.False);
        Assert.That(player.CanFluteJump, Is.True, "A second press is available throughout the normal jump.");
    }

    private GravityBody Moon => System.Array.Find(gravity.bodies, body => body != planets.respawnPlanet);

    private IEnumerator TravelToMoon()
    {
        yield return Launch();
        yield return Press(Key.Space);
        yield return Press();
        yield return WaitForGround();
        Assert.That(planets.CurrentPlanet, Is.EqualTo(Moon));
        yield return null; // Let PlanetManager record this landing before another launch.
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
    public IEnumerator ImmediateSecondJumpEmitsOneBurstAndRejectsThirdJump()
    {
        yield return Launch();
        Assert.That(gravity.IsTraveling, Is.False, "Normal jump retains launch gravity.");
        yield return Press(Key.Space);
        Assert.That(gravity.IsTraveling, Is.True, "Flute jump releases the origin immediately.");
        Assert.That(player.NormalJumpCount, Is.EqualTo(1));
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
        // Walk a quarter turn through real input into the gap between moons, then jump away.
        yield return Press(Key.D);
        yield return new WaitForSeconds(4.2f);
        yield return Press();
        yield return Launch();
        yield return Press(Key.Space);
        // Gravity can return a coasting jump safely. Deliberately thrust outward
        // through the gap to exercise lost-distance recovery instead.
        yield return Press(Key.D);
        float end = Time.time + 13f;
        while (planets.RespawnCount == 0 && Time.time < end) yield return null;
        yield return Press();
        Assert.That(planets.RespawnCount, Is.EqualTo(1));
        yield return WaitForGround();
        Assert.That(planets.CurrentPlanet, Is.EqualTo(planets.respawnPlanet));
        Assert.That(planets.respawnPlanet.SurfaceDistance(player.Body.position), Is.InRange(0.2f, 0.4f));
        Assert.That(player.FluteUsed, Is.False);
    }
    [UnityTest]
    public IEnumerator SingleJumpReturnsToHomeWithoutBoostOrRespawn()
    {
        yield return Launch();
        float highest = 0f;
        float end = Time.time + 3f;
        while (!player.IsGrounded && Time.time < end)
        {
            Assert.That(gravity.CurrentBody, Is.EqualTo(planets.respawnPlanet));
            highest = Mathf.Max(highest, planets.respawnPlanet.SurfaceDistance(player.Body.position));
            yield return null;
        }
        Assert.That(highest, Is.GreaterThan(planets.respawnPlanet.releaseHeight),
            "Regression must exercise the old gravity-release boundary.");
        Assert.That(player.IsGrounded, Is.True);
        Assert.That(player.FluteJumpCount, Is.Zero);
        Assert.That(planets.RespawnCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator SingleJumpCanSteerBothWaysAndStillReturnsToLaunchPlanet()
    {
        foreach (Key direction in new[] { Key.A, Key.D })
        {
            player.Respawn(planets.respawnPlanet, Vector2.up);
            yield return WaitForGround();
            float startX = player.Body.position.x;
            yield return Press(Key.Space, direction);
            yield return Press(direction);
            yield return new WaitForSeconds(0.45f);
            float displacement = player.Body.position.x - startX;
            Assert.That(direction == Key.D ? displacement : -displacement, Is.GreaterThan(0.4f));
            yield return WaitForGround();
            yield return Press();
            Assert.That(gravity.CurrentBody, Is.EqualTo(planets.respawnPlanet));
            Assert.That(planets.RespawnCount, Is.Zero);
        }
    }

    [UnityTest]
    public IEnumerator SingleJumpFromMoonReturnsToMoonWhileSteering()
    {
        yield return TravelToMoon();
        Vector2 before = player.Body.position;
        yield return Press(Key.Space, Key.D);
        yield return Press(Key.D);
        yield return new WaitForSeconds(0.4f);
        Assert.That(Vector2.Distance(before, player.Body.position), Is.GreaterThan(1f));
        yield return WaitForGround();
        yield return Press();
        Assert.That(gravity.CurrentBody, Is.EqualTo(Moon));
        Assert.That(player.FluteJumpCount, Is.EqualTo(1), "Only the outbound transfer used a boost.");
        Assert.That(planets.RespawnCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator LateSecondJumpReversesDescentAndCanReachMoon()
    {
        yield return Launch();
        yield return new WaitForSeconds(0.7f);
        Assert.That(Vector2.Dot(player.Body.linearVelocity, player.Up), Is.LessThan(0f));
        Assert.That(player.IsGrounded, Is.False);
        yield return Press(Key.Space);
        Assert.That(Vector2.Dot(player.Body.linearVelocity, Vector2.up), Is.GreaterThan(6f));
        yield return Press();
        yield return WaitForGround();
        Assert.That(gravity.CurrentBody, Is.EqualTo(Moon));
        Assert.That(planets.RespawnCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator FlightTurnsBeforeCaptureAndLandsWithHeadAwayFromMoon()
    {
        yield return Launch();
        yield return Press(Key.Space);
        yield return Press();
        bool sawTurnInSpace = false;
        float previousAngle = player.Body.rotation;
        float end = Time.time + 4f;
        while (!player.IsGrounded && Time.time < end)
        {
            yield return new WaitForFixedUpdate();
            float angle = Mathf.Abs(Mathf.DeltaAngle(0f, player.Body.rotation));
            if (gravity.IsTraveling && angle > 15f && angle < 165f) sawTurnInSpace = true;
            if (!player.IsGrounded)
                Assert.That(Mathf.Abs(Mathf.DeltaAngle(previousAngle, player.Body.rotation)),
                    Is.LessThanOrEqualTo(player.airRotationSpeed * Time.fixedDeltaTime + 0.1f),
                    "Airborne orientation should turn smoothly, including on capture.");
            previousAngle = player.Body.rotation;
        }
        Assert.That(sawTurnInSpace, Is.True, "Do not wait until capture to turn toward the moon.");
        Assert.That(player.IsGrounded, Is.True);
        Assert.That(gravity.CurrentBody, Is.EqualTo(Moon));
        Vector2 head = Quaternion.Euler(0f, 0f, player.Body.rotation) * Vector2.up;
        Assert.That(Vector2.Dot(head, Moon.UpAt(player.Body.position)), Is.GreaterThan(0.99f));
    }

    [UnityTest]
    public IEnumerator FlightPreservesMomentumSupportsWasdAndCapsSpeed()
    {
        yield return Launch();
        yield return Press(Key.Space);
        yield return Press();
        // Isolate steering from gravity, capture, and recovery in this test only.
        planets.enabled = false;
        foreach (var body in gravity.bodies) body.gravity = 0f;
        player.Body.position = new Vector2(15f, 15f);
        player.Body.linearVelocity = new Vector2(3f, 4f);
        yield return new WaitForSeconds(0.2f);
        Assert.That(Vector2.Distance(player.Body.linearVelocity, new Vector2(3f, 4f)), Is.LessThan(0.05f));
        yield return Press(Key.W);
        yield return new WaitForSeconds(0.2f);
        Assert.That(player.Body.linearVelocity.y, Is.GreaterThan(5f));
        yield return Press(Key.S);
        yield return new WaitForSeconds(0.2f);
        Assert.That(player.Body.linearVelocity.y, Is.LessThan(5f));
        yield return Press(Key.A);
        yield return new WaitForSeconds(0.2f);
        Assert.That(player.Body.linearVelocity.x, Is.LessThan(2f));
        yield return Press(Key.D, Key.W);
        yield return new WaitForSeconds(2f);
        Assert.That(player.Body.linearVelocity.magnitude, Is.InRange(player.maxFlightSpeed - 0.1f, player.maxFlightSpeed + 0.01f));
        yield return Press();
    }

    [UnityTest]
    public IEnumerator FlightGravityRemainsActiveOutsideCaptureZones()
    {
        yield return Launch();
        yield return Press(Key.Space);
        yield return Press();
        planets.enabled = false;
        player.Body.position = new Vector2(15f, 5f);
        player.Body.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.1f);
        Assert.That(gravity.IsTraveling, Is.True);
        Assert.That(player.Body.linearVelocity.x, Is.LessThan(-0.1f), "Both planets should pull from the left.");
    }

    [UnityTest]
    public IEnumerator ManualRecoveryReturnsToLastMoonLandingAndRestoresJumps()
    {
        yield return TravelToMoon();
        Vector2 landing = player.Body.position;
        yield return Launch();
        yield return Press(Key.Space);
        yield return Press();
        yield return Press(Key.R);
        yield return Press();
        yield return WaitForGround();
        Assert.That(gravity.CurrentBody, Is.EqualTo(Moon));
        Assert.That(Vector2.Distance(player.Body.position, landing), Is.LessThan(0.15f));
        Assert.That(player.FluteUsed, Is.False);
        Assert.That(planets.RespawnCount, Is.EqualTo(1));
        yield return Launch();
        Assert.That(player.CanFluteJump, Is.True);
    }

    [UnityTest]
    public IEnumerator OutOfBoundsAndTimeoutRecoverToLastMoonLanding()
    {
        yield return TravelToMoon();
        Vector2 landing = player.Body.position;
        player.Body.position = new Vector2(100f, 100f);
        yield return null;
        yield return WaitForGround();
        Assert.That(gravity.CurrentBody, Is.EqualTo(Moon));
        Assert.That(Vector2.Distance(player.Body.position, landing), Is.LessThan(0.15f));
        planets.maxAirborneSeconds = 0.1f;
        yield return Launch();
        yield return new WaitForSeconds(0.3f);
        yield return WaitForGround();
        Assert.That(gravity.CurrentBody, Is.EqualTo(Moon));
        Assert.That(planets.RespawnCount, Is.EqualTo(2));
        Assert.That(player.FluteUsed, Is.False);
    }

    [UnityTest]
    public IEnumerator HoldingJumpDoesNotAutomaticallyUseFluteBoost()
    {
        yield return Press(Key.Space);
        yield return new WaitForSeconds(1.5f);
        Assert.That(player.NormalJumpCount, Is.EqualTo(1));
        Assert.That(player.FluteJumpCount, Is.Zero);
        Assert.That(player.IsGrounded, Is.True);
        yield return Press();
    }

    [UnityTest]
    public IEnumerator EveryMoonIsReachableFromHomeWithJumpAndFlute()
    {
        var home = planets.respawnPlanet;
        int moons = 0;
        foreach (var moon in gravity.bodies)
        {
            if (moon == home) continue;
            moons++;
            // Start directly beneath each moon, then use the normal jump plus flute boost.
            player.Respawn(home, moon.Center - home.Center);
            yield return WaitForGround();
            yield return Launch();
            yield return Press(Key.Space);
            yield return Press();
            yield return WaitForGround();
            Assert.That(planets.CurrentPlanet, Is.EqualTo(moon), moon.planetId + " should capture the player.");
        }
        Assert.That(moons, Is.GreaterThanOrEqualTo(5));
        Assert.That(planets.RespawnCount, Is.Zero);
    }
}
#endif
