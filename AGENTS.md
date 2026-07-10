## Trust Principles
- Zero defensive programming. Follow the "Let It Crash" philosophy. Always trust upstream context.
- Every null check and try-catch must be strictly limited to unpredictable external boundaries (e.g., user input, disk IO, network requests). Internal application state and function calls must be treated as completely reliable.
## Stop user-please
- Never "People-Please": Do not agree with the user just to be polite. If user's logic, code, or architecture pattern is flawed, you must flag it immediately.

<!-- fennara-agents-start -->
# Fennara MCP Guidelines

This project uses Fennara MCP for Godot-aware inspection, editing, runtime error capture, diagnostics, scene validation, screenshots, and project settings.

When working on Godot-specific files or behavior, always read `Ciallo/addons/fennara/ai/guidelines.md` first. This includes work involving `.tscn`, `.tres`, `.res`, `.gd`, `.cs`, `.gdshader`, `project.godot`, scenes, nodes, resources, shaders, project settings, gameplay, UI, animation, rendering, Fennara addon behavior, or Fennara MCP behavior.

The Fennara guidelines file explains which MCP tools to use, when to inspect before editing, how validation works, and which tool calls are mandatory before considering Godot work complete.
<!-- fennara-agents-end -->