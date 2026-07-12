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

## Script Diagnostics

Never call `script_diagnostics`. In Fennara 0.3.7 it can time out repeatedly
and prevent Godot from closing. Do not retry or broaden an automatic timeout;
use `dotnet build` for C# compilation.

## Runtime Sessions

Never call `runtime_session` or `runtime_script` in this project. In Fennara
0.3.7, runtime-session preflight can pass non-`[Tool]` `.cs` scene scripts to
the GDScript loader and block the launch with `expected type: GDScript`. Use
`dotnet build` and `validate_scene`; ask the user to run the scene manually
when live runtime evidence is required.

## Serialized Files

Prefer Fennara for `.tscn`, `.scn`, `.tres`, and `.res` edits. Never write or
patch these serialized files as text; use `run_scene_edit_script` or another
Godot-aware structured tool.

Check the validation returned by `run_scene_edit_script` and fix reported
scene/resource errors before claiming the edit is complete.

## Project Settings

Prefer `project_settings` over editing `project.godot` manually.

## Fennara Autoloads

Do not remove, rename, disable, or simplify Fennara scripts or autoloads unless
the user asks to uninstall or repair Fennara.

## Godot Documentation

When Godot behavior, APIs, renderer/platform support, or version details are
uncertain, consult current official Godot documentation or another
authoritative online source.

## Tools

### Repository Files

- Normal repository tools: broad reads, search, diffs, and `.gd`, or
  `.gdshader` text edits.

### Scene Inspection

- `get_scene_tree`: scene hierarchy and node paths.
- `get_node_properties`: node properties, scripts, exports, connections,
  resources, and animation data.
- `get_class_info`: native Godot classes, APIs, signals, enums, and constants.

### Scene Editing

- `run_scene_edit_script`: editor-side Godot object-model inspection and edits,
  including nodes, scene-owned resources, standalone resources, and addon APIs.

### Validation And Screenshots

- `validate_scene`: structural checks for 1-10 scenes, followed by a three-second
  headless run for structurally valid scenes. The forced stop's non-zero exit is
  not itself a failure; unset exports are notes and may be intentional.
- `screenshot_scene`: rendered scene image for visual inspection.

### Project Settings

- `project_settings`: `project.godot`, InputMap, autoload, window, rendering,
  physics, metadata, and addon settings.

### Editor

- `scrape_editor`: reads the debugger tree for a scene the user ran in the
  editor.

### File Editing

`write_or_update_file` is only useful when Godot-side path/image handling is
needed. For `.gdshader`, it can reserialize referencing `.tscn`/`.tres` owners;
inspect `reserialized_resources`, `reserialize_warnings`, and
`reserialize_skipped`.

## Scene Edit Scripts

### Inspection Scripts

`run_scene_edit_script` saves inline scripts under `.fennara`. Inspection-only
scripts should log with `ctx.log(...)` and not call `ctx.mark_modified()` or
mutating helpers. If no edit is marked, `modified=false`, `scene_saved=false`
is expected.

### Inherited Scenes

For inherited scenes, Fennara preserves the inherited root or rejects the save
and restores the original file. On `inherited_root_scene=true` with
`scene_saved=false`, the edit did not apply; adjust the returned `script_path`
or narrow the edit. Do not patch the scene text.

## Renderer

Forward+, Mobile, and Compatibility are different renderers, not quality
levels. Compatibility/OpenGL lacks or constrains many RenderingDevice,
screen/depth, post-processing, GI, fog, decal, particle, HDR, MSAA, and texture
features; mobile and web may be more constrained than the editor.
`fennara_status.rendering_context` and `project_settings` expose the active and
configured renderers. `has_rendering_device=false` means low-level
RenderingDevice and compute workflows are unavailable in the connected runtime.

## Tool Call Logs

Tool lifecycle logs are under
`user://.fennara/tool_logs/<session_id>/calls.jsonl`; results and artifacts are
under the adjacent `results/` directory. Events are `received`, `started`,
`completed`, and `failed`; final events link `result_path` and `artifact_path`.

## Timeout Recovery

After a timeout, inspect the latest matching request event before retrying. Use
the linked result if it completed or failed. A request left at `started` may
still be running; wait, then narrow or split it instead of repeating the same
broad call.

## Large Scenes

For large scenes, target known nodes/subtrees, keep output bounded, and use broad
`run_scene_edit_script` scans only when native object access is required.

## Failure States

If the same call fails twice the same way, stop and report the tool, error, and
next step. For connection failures, open this Godot project with Fennara enabled.
For target failures, select this project in the Fennara dock.
