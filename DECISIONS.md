# Project Decisions

## Confirmed

- Repository: `cmdun/fly-me-to-the-moon`
- Repository visibility: private
- Default branch: `main`
- Version control: GitHub with feature branches and pull requests
- Unity version: `6000.6.3f1`
- Rendering: 2D URP
- Target: desktop macOS/Windows
- Art direction: 2D pixel art
- Gravity behavior: forgiving automatic lock near a planet
- Collaboration model: humans orchestrate and review AI-agent work

## Merge policy

No direct work on `main`. Every feature is reviewed and play-tested by a human before merging.

## Decision process

When an agent encounters an architectural choice, it should document the options and stop for human direction rather than silently changing the project design.
