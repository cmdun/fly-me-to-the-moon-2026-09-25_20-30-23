# The listening garden

Twelve optional, physical moon activities now extend the expedition beyond the five golden score pages. Each activity has a unique, permanent reward. Seeds and rescued singers travel safely with you; return to the HOME garden whenever you choose, carrying several rewards at once.

| Activity | Interaction | Variations | Reward |
| --- | --- | --- | --- |
| Bell garden | Listen at the pipes, then shoot the colored bells in the demonstrated order. Walk between shots; listen again whenever needed. | Four phrases, growing from three to six notes, seeded per expedition. | Four musical seeds |
| Prism orchard | Turn brass-handled reflectors with E, following the visible light through every mirror to the crystal. Wake the crystal with E. | Four layouts with three or four mirrors and different initial orientations. | Four crystal bulbs |
| Lantern walk | Gather food beneath the arch, feed the singer, then hold Q while guiding it through three lantern rest stops. Stay close and grounded; shots frighten it. | Four walks, with progressively shorter calling range, fading lanterns, and slower followers. | Four garden companions |

All activities live on separate moons from the five score beacons. Their footprints replace the ordinary central landmark on those moons to keep the walking surface readable. They use the existing art palette and sprite library, with simple geometric reflectors and visible beams. No new packages, world distribution changes, or scene serialization edits are required.

## The reason to return home

The garden is on the opposite side of HOME from the score altar. It begins with empty planters. Bringing back discoveries grows four flowering plants and four crystal clusters, and welcomes four singers. Your progress is saved alongside the main score, without resetting existing saves.

Restore the piano, fill all twelve garden places, and successfully perform at the altar to complete the garden festival. The altar remains replayable; it remembers the best number of perfect notes in the original 24.7-second song.

## Finding content

- **J** opens the field journal and pauses the world. Each card gives an activity, moon number, difficulty, and reward status.
- Select a card to mark that moon on the **M** map.
- Mint diamonds show remaining discoveries. Gold markers remain reserved for the five main score pages.
- Nearby controls have the existing context arrow. **E** opens a short, typed introduction before the activity starts.
- Hints appear only near an activity being played. The normal exploration HUD stays small.

## Reward and failure rules

Wrong bells reset only the current phrase. Echo projectiles cannot solve bell gardens. Reflectors must carry a continuous beam; reaching the receiver alone earns nothing. Singers need food, proximity, grounded guidance, and all three rest stops; approaching the sanctuary from the back cannot skip the walk. Every reward can be earned only once. Incomplete activities can be retried freely, with no currency or lives.

Completed discoveries, home deliveries, festival completion, and best performance persist. An unfinished activity restarts after closing and reopening the game. There is no fast travel or consumable cost.

## Unity review

Project: `moon-world-encounters`, scene: `Assets/Scenes/FullDemo.unity`.
Start Play Mode, press Enter, then J to pick an activity and M to inspect its location. Walk around HOME to the new garden to see its empty or populated state. This update is local; it does not create an application build or publish to GitHub.

## Validation

- Full Unity Play Mode regression suite: **57 / 57 passed** (`../tmp/discovery-full.xml`).
- Final rendered discovery suite: **5 / 5 passed** (`../tmp/discovery-visual-pass.xml`). This completes all four variants of each activity, checks bypass and duplicate reward guards, checks saved-data compatibility, and verifies journal pause and camera framing.
- Final beam and garden polish: **2 / 2 rendered tests passed** (`../tmp/discovery-finish.xml`).
- Screenshots reviewed in `../tmp/discovery-views/`: field journal, marked map, bell gardens, prism chains, lantern walks, and the completed home garden.
- A close-camera failure in the initial visual run was fixed by calculating activity framing from the scene's actual 320×180 pixel reference. The final visual suite passed after that correction.
- Tests run in an isolated local copy with the same source and assets. Test flags disable save writes, preserving the player's expedition.

## Implementation

- `Assets/Scripts/Demo/DemoDiscoveries.cs`: placement, reward inventory, persistence restoration, home garden, and festival progression.
- `Assets/Scripts/Demo/MoonDiscovery.cs`: the three activity types, input rules, art states, and visible beam reflection.
- `DemoGame.cs`, `DemoHud.cs`: journal, contextual interaction, map markers, activity framing, and altar replay.
- `DemoSave.cs`: compatible additions to the existing local save.
- `DemoArt.cs`, `DemoWorld.cs`, `DemoWorldEvents.cs`: activity scenery, physical projectile routing, and the keeper's introduction.
- `Assets/Tests/Demo/DiscoveryTests.cs`: Play Mode behavior and rendered checks.

Restart Play Mode after Unity finishes importing the scripts, then press Enter and J. Completed rewards are permanent; partially played activities restart after reopening the game.
