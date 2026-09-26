# Piano score fragments

Touch a score fragment to collect it and hear its assigned 8-bit piano sound. Click its button in the collection panel to play that sound again. Each unique piece is collected once; replay does not change collection progress.

## Play the demo

Open `Assets/Instruments/Piano/PianoCollectionDemo.unity`, or choose **Tools > Fly Me to the Moon > Play Piano Demo**. Use the existing A/D or arrow movement and Space jump/flute boost.

- Jump and boost outward from 612-B. The C4, E4, and G4 examples are distributed across three different moons in the five-moon prototype.
- Click a collected fragment in the bottom panel to replay it. The arrows page through collections larger than four pieces.
- Fall into space: your collected pieces remain available after the existing respawn.

C4, E4, and G4 are three example sounds, not a required piece count or final composition. They are original, mono, 22,050 Hz, unsigned 8-bit PCM sounds with a short attack and decaying harmonics. Replace them with the team's finished 8-bit music clips in the definition assets. The supplied score-sheet art is a procedural placeholder.

## Integrate with additional moons

No moon names, moon count, gravity manager, or route are referenced by the collection runtime. The demo reuses the existing two-body prototype; the teammate adding moons can place fragments on any number of those new bodies.

1. Add **PianoCollection** to the player root. Unity also adds its **PianoAudioPlayer** and **AudioSource**. Set the source's volume and optional mixer group; leave Play On Awake off. The component plays pickup/replay sounds in 2D so distant moons do not make them inaudible.
2. Ensure there is exactly one active **AudioListener** in the scene, normally on the main camera. The original prototype camera does not have one; the piano demo adds one in its own scene.
3. Create definitions with **Assets > Create > Fly Me to the Moon > Piano Piece**. Set a unique, nonempty `pieceId`, a readable `displayName`, and the piece's `sound` clip. Use a different ID for every distinct score fragment, even if two use the same pitch. Reusing an ID means the same collectible and cannot award or sound twice on pickup.
4. Drag `Prefabs/PianoScorePickup.prefab` onto each desired moon and assign its **Piece** definition. Parent it to the moon and position it just above the surface; its trigger should overlap the player's collider. Rotate the sheet to match the local surface. Adding a moon needs no instrument code changes.
5. Drag `Prefabs/PianoCollectionPanel.prefab` into the scene and assign the player's **PianoCollection** to its **Collection** field. It contains its own overlay canvas and raycaster. Use one **EventSystem** with **InputSystemUIInputModule**; keep UI submit unassigned if Space/south button must remain dedicated to jumping. Replay currently uses mouse clicks.
6. Keep the collector on the existing player object during respawn. The current `PlanetManager` moves this object and therefore preserves the session collection.

The pickup checks for `PianoCollection` on the colliding object or its parents, rather than requiring a player tag or editing `PlayerController`. Sounds play from the player, so hiding a collected pickup does not cut them off. Duplicate world placements disappear when touched without replaying or awarding the piece again. A missing ID or sound leaves the pickup uncollectible until configured.

## Code interfaces

- `PianoCollection.TryCollect(definition)` returns true only for a newly collected, valid piece.
- `PianoCollection.Replay(pieceId)` returns false for an uncollected or unavailable sound.
- `Count`, `Pieces`, and `Contains(pieceId)` expose the session collection.
- `PieceCollected` fires once per unique ID; `PiecePlayed` fires on collection and later replay. Future instrument progression can subscribe without adding moon-specific logic.

The feature uses its own player-owned audio component because no game-wide AudioManager exists yet. Audio routing can be consolidated when that system is introduced. Existing player, gravity, planet, scene, input, package, and Project Settings files are unchanged.

## Scope and persistence

This slice provides score collection and individual sound playback. It does not add a melody puzzle, piano unlock rule, save-to-disk system, or changes to flute travel. Collection survives the existing in-scene respawn and component disable/re-enable; restarting the scene or replacing/destroying the player starts a new collection. If future moons use separate scenes, keep the same collector alive or explicitly transfer its state as part of that scene-loading feature.

UI text uses Unity's built-in runtime font through the project's existing uGUI package. No additional package or font assets are needed.

## Tests

In **Window > General > Test Runner > PlayMode**, run `PianoPlayModeTests` and the existing `PrototypePlayModeTests`.

The piano tests exercise real 2D trigger collection with movement input, assigned sounds on different bodies, duplicate placements, replay-button click dispatch, uncollected replay rejection, respawn persistence, paging beyond four pieces, and non-player overlaps. Automated audio checks verify source playback and clip selection; final timbre and mix should be auditioned by the music team.
