# Full playable demo scene

Open `Assets/Scenes/FullDemo.unity` in Unity **6000.6.3f1** and press Play.

Five moons each have three music-page challenges and a four-lane rhythm performance. Complete them in order to unlock Piano, Bells, Drums, Harp, and Synth. Art is placeholder geometry; audio is synthesized. Progress lasts for the current session.

- A/D: surface movement; Space then Space: jump and musical flight boost.
- WASD: flight steering; mouse + click: shoot notes.
- E: altar interaction or resonator demonstration; shoot resonators **2, 1, 3**.
- A/S/D/F: rhythm lanes; 17 of 24 hits pass. R retries a failed song.
- Tab: instrument; 1–4: play notes; M: map; Esc: pause; R while exploring: recover.

The demo is enabled in Build Settings. Existing prototype and piano scenes remain available. `FullDemoBuilder.BuildMac` builds the demo into `Build/Fly Me to the Moon Demo.app`; builds and generated Unity folders are not versioned.

Validation: 25 PlayMode tests passed in the local demo; the built Mac app restored all five moons with zero validation failures. The automated app playthrough uses real initial flight, projectile interactions, and keyboard rhythm inputs, with respawn checkpoints between challenges. Branch validation is recorded in the PR.
