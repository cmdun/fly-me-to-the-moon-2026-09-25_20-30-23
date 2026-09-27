# Fly Me to the Moon — world encounters

Open `Assets/Scenes/FullDemo.unity` in Unity 6000.6.3f1 and press Play. The world has 24 varied moons, five page beacons, twelve optional collections, a home altar and an Observatory archive. The five page challenges use the real character, circular gravity, movement, and projectiles in the world.

## Five score pages

| Encounter | How to play | Progression and recovery |
|---|---|---|
| Orbital Relay | Carry a spark through three marked gates along a chain of four moons, then press E at the receiver. Use one boost per flight, shared by Space and right click. | A straight transfer, an angled gate, then a moving gate. Blue landing shrines save gate progress. Landing naturally restores the jump budget. Skip shrines for continuous, riskier flight, or R to retry from the last shrine. Missing a gate does not end the attempt. |
| The Sleeping Giant | Shoot each marked bell, release its matching seal, then approach the page while the giant listens elsewhere. | Normal walking is quiet; gravel, jumps, calls, and nearby shots raise visible awareness. Bells calm and distract the giant for nine seconds. Waking returns the player to the beacon, but released seals remain open. |
| Pulse Storm | Jump low waves, shelter from high waves, then shoot both exposed shrine switches. | Three circuits: low waves, reversed high waves, and a combined pattern. Warnings precede each burst. Hits knock the player back and reset the current circuit, preserving earlier circuits. |
| Starlight Shepherd | Open the gate with E, then lead singers to the sanctuary using Q. | Rescue the immediate follower first to wake the sustained-call and silence followers. Nearby shots frighten them. Thorn patches scatter unrescued creatures back to their starting positions. Symbols, movement, and color show their behavior. |
| Duet with Your Echo | C records up to eight seconds of movement, pressure-plate occupancy, and shots. E starts looping playback. Cross the ghost-opened gate and shoot the receiver. | A route trace, visible ghost, and countdown show playback. The second room requires ghost and player pressure plates to overlap. C immediately clears and replaces the recording. Earlier rooms remain completed when retrying. |

Each encounter awards one page, once. The five beacons are on distinct moons. The relay follows three real neighbor links through the scattered field, including their bends. Other encounter hosts and beacon positions remain seeded and varied.

Return all five pages to the altar on 612-B, repair the score through its dialogue, and perform the restored melody (17 of 24 notes). This unlocks the piano. Failed performances preserve the pages and repair.

## Observatory home collection

Twelve non-quest moons contain four bell gardens, four prism orchards and four lantern walks. Completing one packs its musical seed, crystal or singer into the player's cargo. The journey HUD shows the current cargo count, while J opens the Observatory collection journal and lets the player track unfinished moons.

Return to the Observatory on 612-B and press E at its archive to deposit every carried collection. Each deposit fills a visible display around the Observatory and is saved. Depositing the twelfth collection permanently lights the Observatory beacon and triggers a home-planet musical celebration. With the restored piano, a successful altar performance turns that awakening into the full planetwide concert.

## Controls

- A/D: walk around a moon; Space: jump; Space again or right click: the one airborne boost.
- WASD after boosting: steer through space. Right-click recoil pushes opposite the aim.
- Left click: shoot notes without changing movement or consuming the boost.
- E: interact, advance dialogue, operate encounter objects, or end echo recording.
- Q: hold a flute call. C: record or immediately rerecord an echo. R: retry the encounter checkpoint; outside encounters, recover to the last safe landing.
- M: full map. The nearby minimap follows position and keeps north fixed.
- Escape: pause and view controls. Leave on the encounter HUD abandons that attempt.
- 1–7: play C D E F G A B. Tab switches flute/piano after unlocking and landing.
- A/S/D/F: final home-altar performance.

Approaching an exploration interactable shows an arrow; E opens typed instructions. Encounter instructions use short pages. During an encounter, a compact status display, relevant world labels, and a goal pointer replace the former full-screen input boards. Escape pauses encounter clocks and physics. The final altar performance restarts its song after a pause.

## World and implementation

Moons have radii 3.2–5.8 and grow from randomly chosen existing worlds in unrestricted directions. Random candidate positions are weighted toward less crowded areas so the field surrounds home evenly. There are no ring slots, grids, mirrored positions, or evenly spaced spokes. Completed fields must cover all four sides, stay centered within 6 units of home, and leave no empty angular sector larger than 35 degrees. The field reaches at least 45 units in each cardinal direction and fits within a 78-unit radius. Consecutive connections bend by at least 25 degrees to prevent long radial rows. Each moon records a reachable neighbor with a 6.5–8-unit surface gap; all other moons remain at least 6.5 units apart. Single jumps return to the launch planet. Grounded movement follows the curved surface. The flute remains jump 5 / boost 3 / flight cap 9; the unlocked piano remains 6 / 5 / 13.

`DemoGame.WorldSeed = 0` chooses a new seed; a nonzero value reproduces a layout. The existing moonfolk and their multipage conversations remain, with hints updated for the new encounters.

`DemoChallenge` owns a temporary encounter root and delegates to five separate encounter classes. The page beacon hides during an attempt and reappears if you leave without completing it. Ending an encounter removes its gates, receivers, creatures, and ghost without changing the underlying moon. Physical projectile raycasts respect terrain and closed gates. Only active encounters accept receiver hits. `PlanetManager` records a new safe landing after returning from encounter-controlled movement.

`FullDemoBuilder.BuildMac` creates `Build/Fly Me to the Moon Demo.app`. Runtime objects are generated through Unity APIs; no scene YAML, packages, or project-setting migrations are part of this update. The development build supports `-demoSmoke <output-directory>` for an opt-in standalone playthrough and screenshots, using simulated inputs and actual physics/projectiles. Checkpoint respawns are used between encounters; movement within encounters is played through.

Placeholder visuals and synthesized sounds remain. Progress is session-only. These are complete prototype encounters with two echo rooms, three storm circuits, three relay transfers, three singer behaviors, and one shifting giant arena.

## Verification (2026-09-26)

The full Unity Play Mode suite passed 39/39 tests after the balanced scatter update. Checks cover 1,000 generated fields with coverage on every side of home, a centered distribution, no large empty angular wedges, variable home-neighbor counts, no straight parent-chain continuation, safe spacing, connected routes, and valid three-transfer relays. The suite also flies a real round trip to each of the 24 moons, completes all five encounters and the piano unlock, and verifies movement, minimap, and dialogue behavior.

This update is for checking in Unity; no new standalone demo was built. Open `Assets/Scenes/FullDemo.unity`, enter Play Mode, press Enter to start, and M to inspect the map. Stop and restart Play Mode for a new random layout when `WorldSeed` is zero.
