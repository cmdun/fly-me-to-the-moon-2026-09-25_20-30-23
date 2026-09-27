# Musical scenery

Added to HOME and all 24 moons in `Assets/Scenes/FullDemo.unity` at runtime.

- Turquoise bell blossoms and a wind-chime rack.
- Blue keyboard ferns and a fan of piano pipes.
- Green panpipe reeds and a small breath organ.
- Orange drumcap mushrooms and percussion sculptures.
- Pink lyre vines and little harps.
- Violet crystal seedlings and crystal music boxes.
- Songbirds, chime beetles, and leaf-eared seedling creatures.

Walk around a moon to see its rim gardens. Press **1–7** or hold **Q** nearby: plants sway, creatures hop and answer, and musical notes rise. Shooting a note past a plant or sculpture also makes it respond. These are optional scenery interactions; they do not award fragments or alter saved progress.

The sprites are authored from small pixel motifs with a shared palette, point filtering, no mipmaps, and 40 pixels per unit. Their positions are stable for each world's seed. Plants and creatures follow local gravity, including under the moon. They add no colliders. Landmark, beacon, discovery, and HOME garden areas keep a clear margin. Scenery on challenge moons hides for the challenge and returns afterwards.

Implementation: `DemoAmbientSprites.cs`, `DemoAmbientLife.cs`, with small hooks in `DemoArt.cs` and `DemoGame.cs`. Existing world geometry, input, save data, packages, and scene assets are unchanged.

Validation: `AmbientLifeTests` checks coverage on 25 worlds, surface alignment, clearance, sprite settings, musical reactions, pause behavior, and hiding during the orbital relay. Rendered Play Mode captures cover HOME and all six moon themes. The existing `PixelArtTests` and `DiscoveryTests` exercise the surrounding art and activity systems. All 13 combined Play Mode tests passed (`../tmp/scenery-regression.xml`); screenshots are in `../tmp/scenery-views/`.

To review locally, restart Play Mode in **moon-world-encounters / FullDemo** and press Enter. No standalone build is needed.
