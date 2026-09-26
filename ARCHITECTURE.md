# Approved Architecture

Keep the first version small and replaceable. Use Unity components and prefabs instead of a large framework.

## Core systems

- `PlayerController`: movement, jumping, and player-facing input.
- `GravityBody`: planet center, radius, collider, and planet identity.
- `GravityManager`: nearby-planet detection, target selection, and gravity transition.
- `PlanetManager`: current planet, respawn planet, route progression, and completion.
- `CameraController`: smooth player following and readable framing.
- `AudioManager`: centralized landing, gravity-lock, failure, and planet audio.

## Interface rules

- Planet behavior must be reusable through a planet prefab.
- Planet-specific data should be configurable in the Inspector or a data asset.
- Player code should not hard-code individual planet names or scene object references.
- Level layout belongs to the integrator/world-design owner.
- Gameplay agents should use a test scene and placeholder art first.

## Integration rule

The main scene and Project Settings have a single human owner. Agents may create reusable scripts, prefabs, assets, and test scenes within their assigned scope, but must not modify the main scene unless the issue explicitly assigns it.
