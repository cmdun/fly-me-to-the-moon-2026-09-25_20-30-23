# First playable prototype

Open `Assets/Scenes/FirstPlayablePrototype.unity` in Unity **6000.6.3f1**, then press Play.
Alternatively, use **Tools > Fly Me to the Moon > Play Prototype**.

## Controls and route

- **A/D**, **left/right arrows**, or **gamepad left stick**: move clockwise/counterclockwise around the current surface; steer sideways during travel.
- **Space** or **gamepad south button**: jump. Press again after clearing the surface lock zone to use the one flute boost, shown by six musical-note shapes. No third jump is available.
- Home planet 612-B (radius 12) is ringed by five moons. From the starting position, jump straight upward to **Moon**. After landing on its underside, jump outward again to return to 612-B.
- Walk around 612-B to stand beneath any other moon, then jump and flute-boost to reach it. The moons sit at roughly 0°, 60°, 135°, 215°, and 290° clockwise from the spawn point, with 5.5–7 units of open space between the surfaces.
- Jump through a gap between moons to miss a transfer. Moving more than 22 units from every surface, or remaining airborne for 12 seconds, resets the player to home.

Click the Game view if keyboard input is not reaching the player. The moons start outside the camera frame; a moon's edge appears during the jump toward it.

## Implementation

The test scene contains six instances of `Assets/Prototype/Planet.prefab` (612-B and five moons of different sizes and colors), a reusable player prefab, a camera, and two small manager components. Radius, identity, capture/release distances, jump strength, movement, respawn, and positions are Inspector-configurable. Scene references are assigned in the scene; the player contains no hard-coded planet identity or lookup by object name.

`PlayerController` owns input, surface-relative velocity and jump charges. `GravityManager` selects gravity targets with separate capture/release distances. `PlanetManager` owns current/respawn planet and failure recovery. `CameraController` follows the interpolated player in LateUpdate. No progression or audio system is needed for this slice.

Planets and the player use procedural colored meshes, with a texture-free URP shader. Musical notes are short-lived procedural strokes. The URP Pixel Perfect Camera renders at 320×180, 16 pixels per unit, with integer upscaling and letterboxing. Camera antialiasing and post-processing are disabled in this scene. Project Settings are not changed.

The scene builder under `Tools > Fly Me to the Moon > Create Prototype Scene` is a one-time authoring utility. It refuses to overwrite the saved scene. The saved scene and prefabs are the playable deliverables; no setup command is required to play them.

## Play Mode verification

Open `Window > General > Test Runner`, select **PlayMode**, and run `PrototypePlayModeTests`:

1. Spawn on home, move both ways along its circumference, and keep the player centered.
2. Reject a second jump while locked; allow exactly one flute boost during travel, emit notes, reject a third jump, and clean up the burst.
3. Travel from home to the moon and back using real Input System key events, with landing restoring jump charges.
4. Walk a quarter turn into a gap between moons, jump into space, and verify automatic respawn at home.
5. Start beneath each moon in turn and verify that a normal jump plus the flute boost lands on it.

For a manual visual check, watch the circle edges stay crisp, the player rotate to each planet's surface, and the nearby planet enter the view during transfer. Repeat the jump after landing. Try steering during flight and missing the moon deliberately.

### Verified September 26, 2026

Unity 6000.6.3f1 on macOS: **4/4 automated Play Mode tests passed**, with no skipped tests. The first run revealed that the batch runner's unfocused Game view suppressed simulated input. The test fixture now temporarily ignores focus and restores both input settings after each test; the rerun passed. Game input settings were not changed.

The scene was also opened in the visible Unity Editor and run in Play Mode. Manual keyboard jumps reached the moon and returned home. Visual inspection confirmed the player centered on both surfaces, inverted orientation on the moon's underside, crisp pixel outlines, both neighboring planet edges visible during transit, and the colored note burst. No prototype compile errors, shader errors, or runtime exceptions appeared in the visual-session log. Physical gamepad hardware and standalone builds have not been tested.

Unrelated local AI package additions, their generated settings, Unity settings serialization, and solution changes were present during verification and excluded from this feature. The prototype uses only the existing Input System, URP, physics, and Test Framework dependencies.

### Verified September 26, 2026 — larger home and five moons

Unity 6000.6.3f1 on macOS: **5/5 automated Play Mode tests passed**, with no skipped tests. The scene was opened in the Unity Editor and entered Play Mode; the player spawned on top of the larger 612-B with crisp outlines, and Moon 5 was visible beside the home planet. Keyboard input could not be sent to the editor from the automation session, so the manual jump route was not repeated by hand.

## Deliberate limits

Only a circular home and five static moons exist; the moons do not orbit. Travel is zero-gravity coasting with sideways steering; capture applies the destination's radial gravity immediately. The flute boost adds outward speed and a small input-directed sideways impulse. There is no orbit simulation or camera rotation. The failure timeout also rescues a player who becomes stuck in flight. Planets use unit transform scale; change their radius and matching shape dimensions instead of scaling the root.

There are no imported textures, audio, collectibles, unlocking, menus, progression, win state, or desktop build in this prototype. Visuals and numbers are placeholders for human play-testing.
