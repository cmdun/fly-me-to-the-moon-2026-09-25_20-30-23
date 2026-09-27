# Pixel art integration

Open `Assets/Scenes/FullDemo.unity`, press Play, then Enter. The existing **Tools → Fly Me to the Moon → Play Full Local Demo** command opens this scene directly. The world and its art are assembled when Play Mode starts, just like the existing procedural moons and encounters.

## In the game

- One painted home planet and 24 scattered moons, with four moons for each of the six musical themes. The home observatory, house, altar, garden, and moon landmarks follow the circular surface.
- The traveler wears the navy coat, orange scarf, boots, backpack, and seed pouch. Walking, jumping, landing, flute calls, aiming, notes, and recoil use the supplied reference art. Equipment aims independently and switches to keys when the piano is equipped.
- Five golden page beacons replace the block placeholders. Both maps use the corresponding world art and retain their fixed north orientation.
- Existing encounters use matching art: relay sparks and shrines; a sleeping/listening/alert giant and swinging bells; storm waves, shelter, and repair crystals; musical singers and a hinged gate; and the translucent traveler echo, pressed plates, receiver, and gate.
- Bells swing and flowers open after a hit. Gates move when their actual collider opens. Switch colors track real repair state. Singer movement, fear, sleep, and rescue drive their presentation. Nearby piano scenery lights beneath the traveler; other musical landmarks react when a note reaches them.

Physics, flight tuning, seeded moon placement, five-page progression, challenge rules, and score performance are unchanged. Decorative scenery has no colliders and is hidden on the active encounter's moons to keep challenge paths clear. Decorative planting, lantern, drum, and reflector references do not add new quest mechanics.

## Asset organization

`Assets/Resources/PixelArt/` contains 15 original transparent PNG sheets and `catalog.json`. The catalog identifies 215 reviewed sprite regions, with bottom-left pixel coordinates, pivots, and measured pixels-per-unit. The source images are unchanged; `PixelArtLibrary` creates full-rectangle sprites without generating physics shapes. Original reference sheets remain useful for later art refinement.

The importer uses point filtering, no mipmaps, no compression, original dimensions, alpha transparency, and clamped edges. Unity creates and maintains all `.meta` files. To reapply those settings after replacing a source sheet, close the Editor and run:

```sh
unity run . -- -executeMethod FlyMeToTheMoon.Editor.PixelArtImport.Prepare
```

`DemoArt` owns world decoration; `DemoPlayerArt` reads player actions; `DemoEncounterArt` reads encounter state. These classes only change presentation. `DemoGame`, `DemoChallenge`, `DemoWorld`, and `DemoHud` contain the small integration hooks. No scene or shared prefab serialization was changed.

## Checking changes

1. Walk and jump on home, including its sides and underside. Feet and landing effects should follow local gravity.
2. Fire with left-click, then use right-click during a jump. The note and separate instrument should face the aim direction; only right-click spends the boost.
3. Open the map with M. Check all 24 themed moons and five gold objectives against its opaque background.
4. Enter the five page beacons. Bell hits should swing the bell and open its flower; gates should follow the pressure plate or lever; singers and switches should communicate their current state.
5. Finish the existing journey and play at the altar to unlock the piano.

`PixelArtTests` checks every sprite region/import, unchanged world colliders, gravity alignment, equipment animation, pause behavior, and bell/gate/switch reactions. The existing regression suite exercises movement, real input, all five encounters, and the complete restoration journey.

Validation for this integration: the full Play Mode suite passed **42/42** tests. A separate rendered Editor walkthrough completed all five page challenges and the home performance, reported zero errors, and unlocked the piano. Final presentation changes were also checked with the targeted art tests. Captures and machine-readable results are in the workspace's `tmp/art-live-validation`, `tmp/art-full-tests.xml`, and `tmp/art-presentation-tests.xml` (not shipped as game assets).

No standalone build is required. The changes are intended for review directly in Unity.
