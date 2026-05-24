# Ciallo — Agent Instructions

Ciallo is a GPU-powered vector paint program built with **Godot 4.6 + C# + GDExtension (C++)**.

## Build

```bash
# C# (primary — do this first)
dotnet build Ciallo/Ciallo.sln --configuration Debug

# GDExtension (C++, rarely needed — see GDExtension/README.md)
# Run from GDExtension/
scons dev_build=yes target=template_debug platform=windows arch=x86_64
```

Regular contributors only need to build C#. GDExtension contributors should ask @ShenCiao for help.

## Architecture

| Folder | Role |
|---|---|
| `Ciallo/Data/` | Data-only classes — ECS components, static `App*` managers |
| `Ciallo/Command/` | Undo/redo command implementations |
| `Ciallo/Rendering/` | GPU shader materials and mesh setup |
| `Ciallo/GuiControl/` | Godot UI nodes bound to reactive data |
| `Ciallo/Tool/` | Tool implementations (paint, selection, fill, etc.) |
| `Ciallo/Widget/` | Reusable UI widget scenes |
| `Ciallo/SourceGeneration/` | Roslyn source generators |
| `GDExtension/src/` | C++ GDExtension (currently only `Arrangement2D`) |

## Core Concepts

### Frent ECS
All application data lives as **components on Frent `Entity` objects** — there is no class hierarchy for layers, strokes, or brushes. They are entities with different component sets.

- **Document entity**: A per-`World` singleton entity. Access via `world.Document()` or `entity.Document`.
- **Document-level singletons** (`CommandManager`, `SelectionManager`, `BrushManager`, `ToolManager`) are ECS components on the document entity — not static classes.
- **App-level singletons** (`AppDocumentManager`, `AppStrokeBrushLibrary`, `AppPreference`) are `static` classes.
- Data component classes are POCOs tagged with `[DataContract, ToSerialize]` and `[DataMember]` for serialization. They must **not** inherit from Godot types.

### R3 Reactive Extensions
All bindable data uses `ReactiveProperty<T>` and `ObservableList<T>`. Always call `.AddTo(node)` or `.AddTo(entity)` on subscriptions to prevent leaks.

### Command Pattern
All undoable user actions go through the command system:

1. Create a class inheriting `CommandBase`, named `*Cmd` (e.g., `NewStrokeCmd`).
2. Tag it with `[CommandBuilder]` — the source generator adds a fluent method to `CommandBuilder`, stripping `Cmd`: `NewStrokeCmd` → `.NewStroke(...)`.
3. Override `BeforeFirstDo` (one-time setup on first execution), `Do`, and `Undo`.
4. Override `OnDeletedAsDo` / `OnDeletedAsUndo` when the command creates entities or Godot nodes that must be cleaned up when the undo history is pruned.
5. Chain and commit via `CommandBuilder`: `new CommandBuilder(entity).NewStroke().AddToLayerTree(parent).Commit();`

See [Ciallo/Command/CommandBase.cs](Ciallo/Command/CommandBase.cs) and [Ciallo/SourceGeneration/CommandBuilderGenerator.cs](Ciallo/SourceGeneration/CommandBuilderGenerator.cs).

### Layer Tree
Layers are entities carrying a `LayerTreeNode` component that forms a parent–child tree. The document entity is the root. Layer type is determined by which setting component the entity has: `FolderLayerSetting`, `CommonLayerSetting`, etc.

## Key Files

- [Ciallo/Data/AppDocumentManager.cs](Ciallo/Data/AppDocumentManager.cs) — document lifecycle (create, remove, serialize)
- [Ciallo/Command/CommandBuilder.cs](Ciallo/Command/CommandBuilder.cs) — fluent command composition API
- [Ciallo/project.godot](Ciallo/project.godot) — autoloads and input map
- [GDExtension/README.md](GDExtension/README.md) — GDExtension build notes
