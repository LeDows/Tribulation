# Project Agent Guidelines

## Core Principles

Prefer the simplest Unity-native solution that satisfies the request.

- Keep runtime code small, direct, and easy to inspect.
- Use existing Unity components, Prefabs, serialized references, and project conventions before adding custom systems.
- Avoid generated hierarchies, procedural assets, custom rendering, or broad helper frameworks unless the task clearly needs them.
- Fix the current simple implementation before replacing it with a more complex one.
- Preserve user-authored content and unrelated project behavior.

## Gameplay Changes

Only implement gameplay rules the user asked for.

- Do not add unrequested scaling, progression, balancing rules, automation, or hidden behavior.
- Gameplay values should come from the existing data/configuration path when one exists.
- If a new rule seems useful but was not requested, explain the intent and get confirmation first.

## UI And Text

Keep player-facing UI data-driven and localizable.

- Do not hardcode visible labels, titles, units, placeholders, or button text in gameplay/UI code.
- Read display text from the project localization, config, or content data path.
- Present HUD and stats in clear, scannable fields instead of dense concatenated strings.
- UI scripts should organize and bind data; wording should live in content.

## Prefabs And Assets

Treat checked-in Unity assets as the source of truth.

- Inspect existing Prefabs, scenes, ScriptableObjects, and serialized fields before changing code paths.
- Apply targeted edits to the requested object, component, reference, transform, layout, color, or value.
- Do not regenerate, overwrite, or rebuild existing Prefabs or scenes from scripts unless explicitly requested.
- Preserve hierarchy structure, object names, component order, serialized references, file IDs, overrides, and unrelated manual edits whenever possible.
- Prefer checked-in assets referenced by Prefabs over runtime-generated assets for final gameplay/UI content.
- Runtime code should instantiate and configure project assets through the existing loading/catalog pattern.

## Visual Debugging

When something is invisible, misplaced, clipped, incorrectly layered, or the wrong size, inspect serialized layout and asset data first.

- Check active state, parent, transform, anchors, pivot, size, scale, rotation, render mode, sorting, references, and canvas/layout components.
- Treat Inspector screenshots and serialized asset values as primary evidence.
- Investigate refresh logic, camera-facing behavior, materials, render queues, or custom drawing only after the Prefab/layout data looks sane.

## Performance Regression Prevention

Before changing spawning, pooled runtime objects, pause/state handling, HUD or world-space UI, physics movement, or URP settings, read `Docs/Unity-Performance-Guardrails.md`.

- Repeated gameplay paths must not perform synchronous `Resources.Load`, raw `Instantiate`/`Destroy` churn, `renderer.material` access, or scene-wide `FindObjectsByType` queries.
- Use the existing runtime catalog, pools, active registries, reusable buffers, hierarchy reservation, and `MaterialPropertyBlock` path. Pool acquire/release must be paired and reused objects must reset all life-specific state.
- `Time.timeScale = 0` does not stop `Update`, `LateUpdate`, trigger callbacks, UI rebuilds, or rendering. Simulation callbacks must gate on `GameManager.IsSimulationRunning`; paused frame limiting must restore the previous running target.
- Verify the actual scene and pipeline asset references before changing render settings. In this project the active PC volume is `Assets/Settings/SampleSceneProfile.asset`, not the similarly named default profile.
- Do not solve performance by silently changing gameplay. Pickup merging, pickup caps, projectile caps, despawn rules, or reward relocation require explicit approval.
- After a performance change, run Unity compilation and the complete EditMode suite, check XML and asset diffs, then re-profile a Development Build without Deep Profile for 300-600 representative frames when practical. If any validation cannot run, report why and leave the missing verification explicit.

## Completion Checklist

Before finishing asset or Prefab-related work:

- State which assets or Prefabs were modified.
- Confirm unrelated asset content was left untouched.
- Run a Unity refresh/compile/test when practical, or report why it could not be run.
