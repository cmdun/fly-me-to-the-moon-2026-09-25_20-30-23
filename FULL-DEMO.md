# Fly Me to the Moon — exploration demo

Open `Assets/Scenes/FullDemo.unity` in Unity **6000.6.3f1** and press Play. The playable build scene uses the existing prototype controller with demo-specific tuning. Original prototype and piano test scenes remain available.

## Journey

Start on 612-B with a flute. E at the nearby home altar explains the missing score through a typed dialogue. Three page challenges are assigned to different moons, one in each ring, and can be completed in any order:

- Ruins: find and touch the page.
- Floating page: shoot it with a musical note while grounded.
- Echo stones: interact to hear the sequence, shoot stones 2, 1, 3, then touch the revealed page. A wrong note resets only the sequence.

Bring all three pages home, interact with the altar to **repair the score**, then perform the rhythm challenge. Hit 17 of 24 notes to unlock the piano. Failure preserves pages and repairs. Only flute and piano are available.

Normal exploration shows only the current planet, restoration progress, equipped instrument, and brief collection feedback. The nearest interactable gets an arrow within 2.1 world units on the same planet. E opens dialogue; E/Enter or the dialogue button first reveals the full line, then activates its choice. Escape dismisses dialogue without selecting its action.

## Controls (also in the Escape menu)

| Input | Action |
|---|---|
| A/D | Walk around the surface and steer during a normal jump |
| Space | Normal jump; a second press uses the airborne boost |
| WASD in flight | Steering thrust |
| Mouse + left click | Aim and fire; an airborne shot uses the same second-jump budget and recoils opposite the aim |
| E | Interact or advance dialogue |
| 1–7 | Play C, D, E, F, G, A, B with the equipped instrument |
| Tab | Switch flute/piano after unlocking; land before switching |
| M | Toggle the live map and page destinations |
| R | Recover to the last safe landing; on failed rhythm results, retry |
| Esc | Pause and view controls; resume restarts a paused song with a count-in |
| A/S/D/F during rhythm | The four note lanes |

Each flight allows **one** airborne boost, shared between Space and a mouse shot. Further airborne shots and boosts are blocked until landing. Grounded shooting never consumes it. Keys 1–7 play audio without projectile recoil.

The flute uses jump speed 5, boost 3, flight speed cap 9, and steering acceleration 4. The unlocked piano uses 6, 5, 13, and 6 respectively. The legacy prototype scene keeps its original tuning. Single jumps stay attached to their launch planet.

## Generated galaxy

Each new journey generates **18 moons in three rings**, with six moons per ring. Ring radii are 16.5, 22, and 27.5 units around the home planet. Random rotation, bounded angle jitter, moon sizes, and challenge assignments vary the layout while preserving non-overlapping bodies and short radial travel connections. The three challenge hosts are distinct and selected independently within their rings.

`DemoGame.WorldSeed = 0` selects a new seed each journey. Set a nonzero seed on the Full Demo component for a reproducible layout. `DemoGame.Seed` records the active seed. Progress is session-only; New journey clears it and regenerates the galaxy. For a fixed seed, the same layout is recreated.

## Build and checks

`FullDemoBuilder.BuildMac` builds `Build/Fly Me to the Moon Demo.app`, temporarily applying a windowed Mac demo profile and then restoring project settings. Generated Unity folders and app binaries are not versioned.

The PlayMode suite has 30 tests, including 1,000 randomized layouts, actual travel to every generated moon with the starter flute, lower jump height, recoil direction, recapture after inward/sideways recoil, shared boost limits, typed dialogue, restoration gating, all seven note frequencies, failure/retry, restart, and existing movement/piano regression tests.

The development app supports `-demoSmoke <output-directory>` for an opt-in standalone playthrough and screenshots. It checks real flight and keyboard inputs plus all page interactions, repair, performance, piano unlock, and seven-note playback. Respawn checkpoints position the player between interaction tests. Normal play has no test controls enabled.

Visuals remain placeholders and sounds are synthesized. The generation is bounded ring variation, not unrestricted random placement. The rhythm challenge intentionally retains four lanes; free play has seven notes.
