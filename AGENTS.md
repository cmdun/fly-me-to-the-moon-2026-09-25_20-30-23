# Agent Instructions

## Mission

Help the human team build a small, playable 2D pixel-art Unity game called **Fly Me to the Moon**. Humans own the game direction, architecture, priorities, review, testing, and merges.

Read these files before working:

- `PROJECT_BRIEF.md`
- `ARCHITECTURE.md`
- `DECISIONS.md`

## Git workflow

- Never commit directly to `main`.
- Never push directly to `main`.
- Work on one focused feature branch per task.
- Pull the latest `main` before starting work.
- Keep commits small and focused.
- Do not merge pull requests or delete branches.
- Do not force-push or rewrite shared history.
- Report the branch name, commit(s), changed files, tests, and known limitations when finished.

Use branch names such as:

- `feature/<short-description>`
- `fix/<short-description>`
- `art/<short-description>`
- `audio/<short-description>`
- `docs/<short-description>`

## Required save, test, and pull-request workflow

- Every meaningful update must be saved in a focused Git commit.
- Every completed update must be pushed to its feature branch.
- Every completed update must be submitted through a pull request into `main`.
- Never push directly to `main`.
- Before pushing a completed update or opening its pull request, open and run the Unity project in Play Mode.
- Test the behavior changed by the task, not only whether the project opens.
- If the project fails to run or the feature does not work, debug and retest before pushing or opening the pull request.
- Include the test steps and result in the completion report and pull request description.
- Keep commits small enough to review, trace, and revert.
- Do not merge your own pull request; the human integrator performs the review and merge.

## Scope control

Work only on the assigned GitHub issue. Before editing, identify the allowed files, dependencies, and acceptance criteria.

Do not, without explicit human approval:

- Change the game concept or MVP priorities.
- Change the Unity version or add packages.
- Rewrite approved architecture or public interfaces.
- Edit the main gameplay scene.
- Modify Project Settings, input settings, or shared prefabs outside the task.
- Rename or delete shared assets, scripts, or public classes.
- Add stretch features before the MVP is playable.

If requirements conflict or a decision is needed, stop and ask the human owner.

## Unity safety

- Preserve every `.meta` file.
- Do not commit `Library/`, `Temp/`, `Logs/`, `obj/`, or build output.
- Prefer prefabs, ScriptableObjects, and small test scenes over duplicated scene content.
- Avoid editing shared Unity scenes; use a test scene unless the task explicitly owns the scene.
- Do not make broad automatic rewrites to Unity serialization files.

## Completion report

Before requesting review, provide:

1. What changed
2. Files changed
3. How to test it in Unity
4. Tests or checks run
5. Known limitations
6. Decisions requiring human approval

An agent task is complete only when a human can review and play-test it.
