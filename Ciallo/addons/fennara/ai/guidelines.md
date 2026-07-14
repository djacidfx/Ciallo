# Fennara MCP Guidelines

## Connection

Fennara requires Godot 4.x open on this project, the addon enabled, and this
project selected in the Fennara dock. `fennara_status` reports the connection,
active target, available tools, and renderer context. If multiple projects are
open, calls go to the selected target.

## Tool Discovery

The MCP server may appear as `fennara` or Cursor's internal `user-fennara`;
they are the same server. Read each tool's live schema for its exact arguments
and result shape. If tools are deferred, search with a limit of at least 20:

```text
fennara_status read_file write_or_update_file run_scene_edit_script get_scene_tree screenshot_scene get_node_properties get_class_info validate_scene project_settings runtime_session runtime_script scrape_editor
```

Use the live tool schemas as the source of truth for arguments, limits, script
contracts, result fields, and recovery procedures.

## Tool Choice

- Scene structure and properties: `get_scene_tree`, `get_node_properties`.
- Native Godot APIs: `get_class_info`.
- Scene and resource edits: `run_scene_edit_script`.
- Script and shader diagnostics: `script_diagnostics`.
- Live behavior: `runtime_session` with `runtime_script`.
- Visual verification: `screenshot_scene`.
- Project settings and InputMap: `project_settings`.

## Runtime Sessions

Use `runtime_session` with `runtime_script` when live scene state, input-driven
behavior, or runtime captures provide evidence that build and validation cannot.
Start or inspect the managed session first, pass its `session_id` to each
`runtime_script` call, and stop the session when finished.

Verify behavior from observed state changes; a successful helper call only
proves that the helper ran.

## Serialized Files

Prefer Fennara for `.tscn`, `.scn`, `.tres`, and `.res` edits. do not write or
patch these files as text; prefer using other Godot-aware structured tool.

Direct text repair is allowed only when an existing text resource is already
unloadable or contains merge conflicts and structured tools cannot operate.

## Project Settings

Prefer `project_settings` over editing `project.godot` manually.

## Source Files

Normal repository tools may read and edit source files. Prefer
`write_or_update_file` for `.gd` when immediate Godot diagnostics are useful,
and for `.gdshader` when diagnostics and referencing resource reserialization
are useful. After related C# edits, use `dotnet build` or one project-wide
`script_diagnostics` call.

## Fennara Autoloads

Do not remove, rename, disable, or simplify Fennara scripts or autoloads unless
the user asks to uninstall or repair Fennara.

## Godot Documentation

When Godot behavior, APIs, renderer/platform support, or version details are
uncertain, consult current official Godot documentation or another
authoritative online source.

## Renderer

Forward+, Mobile, and Compatibility are different renderers. This project only support Forward+.

## Failure States

If the same call fails twice the same way, stop and report the tool, error, and
next step. For connection failures, open this Godot project with Fennara enabled.
For target failures, select this project in the Fennara dock.