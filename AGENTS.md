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

## Completion Checklist

Before finishing asset or Prefab-related work:

- State which assets or Prefabs were modified.
- Confirm unrelated asset content was left untouched.
- Run a Unity refresh/compile/test when practical, or report why it could not be run.
