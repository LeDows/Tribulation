# Project Agent Guidelines

## Prefab Editing Policy

This Unity project treats checked-in Prefab assets as the source of truth.

When changing runtime objects, UI layout, HUD layout, menus, or settlement screens:

- Do not regenerate or overwrite existing Prefabs from code.
- Do not add editor menu items or scripts that rebuild `Assets/Resources/Prefabs` wholesale.
- Do not use runtime-style `new GameObject`, `CreatePrimitive`, or full hierarchy recreation as the editing path for existing Prefabs.
- Read the existing `.prefab` asset or inspect the Prefab hierarchy through Unity tooling before making changes.
- Apply targeted edits only to the requested GameObject, Component, serialized field, anchor, size, position, color, reference, or text value.
- Preserve manual edits, object names, hierarchy structure, component order, serialized references, fileIDs, and unrelated overrides whenever possible.
- If a requested change requires replacing a hierarchy, explain why first and prefer creating a new sibling/child over deleting and rebuilding user-authored content.

For UI work:

- `Assets/Resources/Prefabs/GameUi.prefab` is the canonical UI Prefab.
- Main menu, HUD, and result screen changes must be made by modifying the existing Prefab structure.
- Canvas and layout fixes should update existing `RectTransform`, `CanvasScaler`, `Text`, `Image`, and `Button` serialized fields directly.
- Generated helper scripts may be used only as one-off migration tools when explicitly requested, and they must be non-destructive by default.

For gameplay Prefabs:

- `Assets/Resources/Prefabs` contains the canonical runtime Prefabs.
- Runtime scripts should load and instantiate Prefabs through the project catalog/loading path, then configure gameplay data.
- Runtime code should not create final gameplay objects from primitives except for temporary diagnostics explicitly removed before completion.

Before finishing a Prefab-related change:

- Confirm which Prefab files were modified.
- Confirm that unrelated Prefab content was left untouched.
- Run a Unity refresh/compile or otherwise report why it could not be run.
