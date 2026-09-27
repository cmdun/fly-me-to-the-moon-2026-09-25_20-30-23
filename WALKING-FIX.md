# Alternating walking animation

The old walk loop used the piano-interaction reference strip. It repeated similar leading-leg poses and did not provide a complete alternating gait.

`DemoWalkCycle.cs` animates two independent legs using regions of the existing traveler texture. The feet are half a cycle apart: one plants while the other lifts and passes forward. Each leg bends at the knee, boots stay level with the local surface during contact, and the entire pose follows the player's local gravity and facing direction.

The follow-up reduces normal walking cadence from approximately five to 2.5 steps per second. Smoothed movement speed drives the cycle, capped at 2.6 steps per second. Contact lasts 60% of each leg's cycle, giving a short double-support period. Swing height is reduced from .12 to .065 units; a rounded arc eases lift-off and landing, with a small boot roll during swing. The torso bob is reduced by two thirds, and only the torso leans, keeping planted boots aligned to the surface. Stride amplitude eases in and out over .12 seconds instead of appearing or disappearing at full stretch.

`DemoPlayerArt.cs` switches between the walking rig and the existing idle, jump, landing, and shot poses. Calling with Q or playing notes while moving keeps the walk cycle and uses the appropriate flute upper-body pose. Shooting still stops grounded movement as requested.

No movement physics, scene, original image, texture import, or save-data changes. The crop coordinates belong to the current character sheet; a future replacement sheet will need updated rig regions.

Validation: 9 Play Mode checks passed (0 failed), including `WalkingAnimationTests` and existing `PixelArtTests`. Tests drive the real controller in both directions at four surface orientations, verify both feet lead and lift, verify a foot stays planted, and check idle, pause, shots, jumping, and walking while calling. A timed controller test checks the slower cadence, lower foot lift, stopping and restarting. Eight poses and the mirrored pose were rendered; contact, passing and mirrored captures were visually inspected. This remains a stylized rig of the existing artwork, not a replacement hand-drawn animation sheet.

Changed files: `Assets/Scripts/Demo/DemoWalkCycle.cs`, `Assets/Scripts/Demo/DemoPlayerArt.cs`, `Assets/Tests/Demo/WalkingAnimationTests.cs`, and this document. No decisions need approval.

Results: `../tmp/walking-natural.xml`; captures: `../tmp/walking-views/`.

Review in the `moon-world-encounters` project, `Assets/Scenes/FullDemo.unity`. If already playing, exit Play Mode and allow the scripts to recompile before playing again. Press Enter to continue, hold A or D to walk, release to check the stop transition, and hold Q while walking to check the combined flute animation. Changes are local on `codex/walking-animation`; no build or GitHub operations.
