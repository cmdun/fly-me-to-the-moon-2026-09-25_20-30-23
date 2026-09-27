# Gameplay and presentation pass

Open `Assets/Scenes/FullDemo.unity` in this Unity project and press Play. The scene generates its world when Play starts. No standalone application is required.

## Controls and feel

- A/D walks around the current body's rim. Space jumps, then Space or right mouse supplies one airborne boost.
- Left mouse plants the player's feet for the flute phrase. Holding it repeats the phrase. Walking resumes when the phrase finishes. In flight, shooting suppresses steering while gravity and existing momentum continue, so shooting cannot be used to hover. Right mouse retains directional recoil and the shared boost budget.
- The playing pose brings the flute to the mouth. Between actions, it floats beside the explorer. The unlocked piano is a portable floating keyboard with responding keys.
- E uses nearby musical toys and opens conversations at beacons, creatures and the altar. Q is the shepherd call. R retries an encounter or recovers during exploration. M opens the map. Escape shows all controls.
- The altar uses **D / F / J / K**. Its original piece, **Moonlit Home**, lasts approximately **24.7 seconds**, including count-in and release. It has an original swing melody, bass, chord accompaniment and soft percussion. It is not the Utada Hikaru recording.

## World

One home planet and 24 irregularly scattered moons retain their existing gravity and collision geometry. A custom pixel surface shader uses a consistent 32 pixels per world unit, continuous rims, restrained crater shapes and six compact color palettes. The original oversized terrain pictures no longer determine the walkable surface's appearance.

Each moon has a musical landmark and three small growing plants. Bells swing and sound; floor keys play as the traveler crosses them; drums compress and launch; harp strings open a hinged chime gate; woodwind calls attract a small singer; crystals illuminate their companion. Plants respond to music and E. Home has a usable drum, and its garden grows with recovered pages.

Object names have been removed from the world overlay where the artwork already explains the object. Near-range action hints, creature response rules, the current challenge goal and necessary meters remain.

## Challenge rules

| Challenge | Required play | Bypass protection |
|---|---|---|
| Sleeping Giant | Distract with the first marked bell, stand quietly to open its seal, repeat with the other bell, then take the page before the distraction ends. | Approaching from the back cannot release the page; the wrong bell, movement, high awareness or an expired distraction cannot open a seal. A wake resets the nest. |
| Pulse Storm | Survive wave patterns, approach the exposed switches and repair them in their lit order, then retreat. | Evading the actual waves charges the shrine, so waiting outside the storm cannot bypass the defenses. Covered, uncharged, distant, airborne, wrong-order and ghost shots do not repair. Hits reset the current circuit. Finishing a circuit no longer teleports the player to safety. |
| Starlight Shepherd | Open the sanctuary, guide each singer using its distinct sound rule, and let it settle safely. | Both directions respect the closed gate; merely reaching the sanctuary without being guided or while frightened does not count. |
| Orbital Relay | Carry the spark through the ordered gates and land at optional checkpoints. | The receiver cannot skip gates; crossings must be airborne, forward-moving, and physically plausible. |
| Duet with Your Echo | Record a route to the first plate, cross during playback, then coordinate both plates in the second room. | The receiver requires the actual recorded ghost, the correct side of the gate and the second plate when applicable; jumping around the gate or firing a ghost shot cannot complete it. |

## Local progress

Completed pages, repaired score, unlocked piano, world seed and the last saved landing are kept in a local expedition slot. The title offers Continue and New journey. Test runs and the validation harness do not read or overwrite that slot. Unfinished encounters restart safely.

## Validation

Unity Play Mode regression tests cover movement and boost budgets, all five challenges, seeded worlds, UI flow, the complete score restoration and piano unlock, imported sprite regions, surface shaders and local gravity. A separate rendered Play Mode journey checks the actual Game view. Test results and captures are stored outside Assets in the workspace's `tmp` directory.

Verified on September 26, 2026:

- Full regression run: **49/49 passed** (`polish-full-tests.xml`).
- Final storm, audio, drum, persistence and full-journey retest: **9/9 passed** (`polish-final-tests.xml`).
- Normal-frame-rate giant route across four world seeds, page bypass protection and surface import checks: **3/3 passed** (`polish-layout-tests.xml`).
- Rendered keyboard placement, including sideways/upside-down gravity: **1/1 passed** (`polish-piano-tests.xml`).
- Full rendered journey: **passed, 0 reported errors, 24 moons, 5 pages, piano unlocked**, seed `1413904313` (`polish-live-final/result.json`).
- Final piano screenshot: `tmp/art-validation/pocket-piano-final.png` in the parent workspace.

The rendered harness uses explicit test arguments and never saves progress to the local expedition slot. Its input stays active when a different Editor window is focused.

Changes are local to branch `codex/gameplay-polish`; no GitHub push or standalone build is part of this pass.
