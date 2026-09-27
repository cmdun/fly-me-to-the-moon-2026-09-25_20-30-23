# Alternating walking animation

The old walk loop used the piano-interaction reference strip. It repeated similar leading-leg poses and did not provide a complete alternating gait.

`DemoWalkCycle.cs` now animates two independent legs using regions of the existing traveler texture. The feet are half a cycle apart: one plants while the other lifts and passes forward. Each leg bends at the knee, boots stay level with the local surface during contact, and the upper body has a small vertical movement. Distance traveled drives the cycle, and the entire pose follows the player's local gravity and facing direction.

`DemoPlayerArt.cs` switches between the walking rig and the existing idle, jump, landing, and shot poses. Calling with Q or playing notes while moving keeps the walk cycle and uses the appropriate flute upper-body pose. Shooting still stops grounded movement as requested.

No movement physics, scene, original image, texture import, or save-data changes. The crop coordinates belong to the current character sheet; a future replacement sheet will need updated rig regions.

Validation: 8 Play Mode checks passed, including the new `WalkingAnimationTests` and existing `PixelArtTests`. Tests drive the real controller in both directions at four surface orientations, verify both feet lead and lift, verify a foot stays planted, and check idle, pause, shots, jumping, and walking while calling. All eight poses and the mirrored pose were rendered and reviewed.

Results: `../tmp/walking-regression.xml`; captures: `../tmp/walking-views/`.

Review in the `moon-world-encounters` project, `Assets/Scenes/FullDemo.unity`. Press Enter to continue, hold A or D to walk, and hold Q while walking to check the combined flute animation.
