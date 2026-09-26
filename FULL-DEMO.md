# Fly Me to the Moon — exploration demo

Open `Assets/Scenes/FullDemo.unity` in Unity **6000.6.3f1** and press Play. The playable build scene uses the existing prototype controller with demo-specific tuning. Original prototype and piano test scenes remain available.

## Journey

Start on 612-B with a flute. E at the home altar explains the missing melody through typed dialogue. Exactly **five** golden score-page beacons appear on five distinct randomly selected moons. Large sheets with musical staffs and gold map markers identify them. Approach a beacon for its context arrow, press E for instructions, then begin its challenge. They can be completed in any order:

| Page | Challenge | Requirement |
|---|---|---|
| Echo memory | Watch/listen, then repeat with 1–7 | Six notes; two mistakes fail; 24 seconds after the preview |
| Pulse passage | A/S/D/F timing lanes | At least 10/12 hits within ±0.12 seconds; wrong keys also count |
| Shooting stars | Left-click moving targets | Eight hits in 16 seconds; three misses fail |
| Note cipher | Follow numbered interval clues with 1–7 | Six answers, wrapping between 7 and 1; two mistakes or 22 seconds fails |
| Silent maze | WASD through three alternating openings | Reach the exit in 22 seconds; walls reset position; three collisions fail |

Return from a successful challenge to collect its page. Touching or shooting a beacon cannot bypass the challenge. Failure preserves other pages, and E at the beacon starts a fresh attempt. Escape pauses challenge time; Leave abandons the attempt.

Bring all five pages home, interact with the altar to **repair the score**, then perform the restored melody. Hit 17 of 24 notes to unlock the piano. Failed performances preserve the pages and repair. Only flute and piano are available.

## Living encounters

612-B is home to **Lyra, Keeper of 612-B**, who introduces the Hush: a silence that taught the moons to forget their music. Lyra points toward the five score-page beacons and updates her dialogue as pages return, the score is repaired, and the piano awakens.

Eight optional moonfolk appear on eight distinct moons, with two encounters at each world depth. Their locations are chosen from the same world seed as the galaxy, so a fixed seed recreates both the moon layout and conversations. Each creature teaches one part of play through a short, typed conversation:

- Pip explains surface movement, jumping, and the shared airborne boost.
- Nim explains the map, ring-to-ring routes, and recovery.
- Mote explains separate left-click shooting and right-click airborne recoil.
- Tink explains the six-note echo-memory challenge.
- Vesper tells how the Hush scattered the melody.
- The Bramble Choir explains free-play notes, score performance, and instrument switching.
- Cadence explains the A/S/D/F pulse challenge and accurate timing.
- Sable introduces the five challenge types, the silent maze, and challenge pausing.

First conversations have three pages and leave the creature visibly changed after the player acknowledges them. Later conversations are brief reminders. Encounters are optional and independent of score collection, so adding, moving, or restyling moons does not change progression.

Normal exploration shows the current planet, restoration progress, equipped instrument, a nearby minimap, and brief collection feedback. The nearest interactable on the same planet gets an arrow within 2.1 units. E opens dialogue; E/Enter or the dialogue button first reveals the line, then advances the conversation or activates its final choice. Escape dismisses dialogue without selecting its action.

## Controls (also in the Escape menu)

| Input | Action |
|---|---|
| A/D | Walk around the surface; steer during a normal jump |
| Space | Normal jump; a second press uses the airborne boost |
| WASD in flight | Steering thrust |
| Left click | Aim and shoot notes; never changes movement or consumes a jump |
| Right click in the air | Recoil opposite the aim; consumes the same boost as a second Space press |
| E | Interact or advance dialogue |
| 1–7 | Play C, D, E, F, G, A, B |
| Tab | Switch flute/piano after unlocking; land before switching |
| M | Toggle the complete world map |
| R | Recover to the last safe landing; retry a failed final performance |
| Esc | Pause and view controls; a paused final performance restarts on resume |
| A/S/D/F during rhythm | The four note lanes |

Each flight allows **one** airborne boost shared by Space and right click. Left-click shooting remains available before and after that boost. Right click on the ground has no effect. Keys 1–7 play audio without recoil.

The flute uses jump speed 5, boost 3, flight speed cap 9, and steering acceleration 4. The piano uses 6, 5, 13, and 6. The legacy prototype keeps its original tuning. Single jumps return to the launch planet. Grounded walking integrates an arc around the surface, preventing tangent motion from carrying the character off a moon.

## Generated world and maps

Each new journey generates **24 moons** with radii **3.2–5.8 units**, compared with the earlier 1.6–1.95. Four loose depths of six moons form irregular outward routes. Moon positions bend away from uniform radial spokes, with varying radii and route lengths. Every moon has an inward connection with a **6.5–8-unit surface gap**, and all pairs keep at least 6.5 units of clearance. These gaps require a double jump; normal jumps retain their launch planet. The outer extent exceeds 65 units, compared with the previous approximately 30.

The minimap follows the player's position, keeps north fixed at the top, shows nearby moon sizes and uncollected page beacons, and keeps the player centered. Its half-width is 32 world units. M opens an automatically fitted whole-world map with moon numbers, remaining page markers, and your current position.

`DemoGame.WorldSeed = 0` chooses a new seed each journey. Set a nonzero seed for a reproducible layout. `DemoGame.Seed` records the active seed. Progress is session-only; New journey clears it and regenerates the world. For a fixed seed, the same layout and encounter moons are recreated.

## Build and verification

`FullDemoBuilder.BuildMac` builds `Build/Fly Me to the Moon Demo.app` using a temporary windowed Mac profile, then restores project settings. Generated Unity folders and app binaries are not versioned.

The PlayMode suite covers 1,000 randomized layouts and encounter plans, outbound and return starter-flute routes for every moon, full-circle surface walking, separate mouse bindings and boost limits, minimap tracking, five challenge rules and actual input flows, multi-page and repeat dialogue, home-only restoration, failure/retry, piano unlock, restart, and existing movement/piano regressions.

The development app supports `-demoSmoke <output-directory>` for an opt-in standalone playthrough with screenshots. It uses actual keyboard/mouse input for the five challenges and final performance, and checks flight, recoil, restoration, and piano unlock. Respawn checkpoints position the player between interaction tests. Normal play has no test controls enabled.

Visuals are placeholders, sounds are synthesized, and progress is not saved across launches. The final performance has four lanes; free play and the memory/cipher challenges use all seven notes.
