# Roadmap
Below is the roadmap for the Demo version. Will make a release on steam and start version "v0.1 EA" after finishing it.

- [x] Document/world manager
- [ ] Serialization
  - [ ] Export to Godot
  - [ ] .Ciallo binary format
- [x] Command undo/redo system
- [ ] Property undo/redo
- [ ] Tool system
  - [x] Infrastructure
  - [ ] Brush tool
    - [x] Basic interaction
    - [ ] With brush engine
    - [ ] Paint stabilizer
    - [ ] Resize brush interactor
  - [ ] Paint fill tool
  - [ ] Vector fill tool
    - [x] CGAL C++ code
    - [ ] C# tool code
  - [ ] Selection/move tool
    - [ ] Line binding system (Bézier curve only)
      - [x] Bézier curve geometry
    - [ ] Design
    - [x] Polyline overlay rendering
  - [ ] Lasso tool
- [ ] Layer system
  - [x] Add, delete
  - [x] Rename
  - [x] Reorder
  - [ ] Merge, split
  - [ ] Import image as a layer
- [ ] Stamp brush engine
- [ ] Localization
  - [x] Infrastructure (ai translation)
  - [ ] Complete
---

# Ciallo Contributing Guide

## Introduction
This guide is not yet complete.
The current version seems like Shen's personal book note, but it aims to be a comprehensive guide for developing Ciallo.

## Basic setup
### How to build

Ciallo is built on Godot. Building the core part of Ciallo is the same as building a standard Godot C# project:

- Set up Godot 4.4.1 with .Net9. You can follow an arbitrary [video guide](https://www.youtube.com/watch?v=7nExKQn1CAw), but pay attention to the version.
- Open the `Ciallo/project.godot` file with your Godot editor, then build and run.
- Enable the "Embedded game size stretches..." option in the game run window.
  
![](/.github/EnableStretch.png)

### IDE
In theory, you can use any IDE supporting C#. Follow the Godot [guide](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html#configuring-an-external-editor) to link the Godot editor with your IDE.

However, I suggest using JetBrains [Rider](https://www.jetbrains.com/rider/), which is free since 2024 and offers comprehensive productivity support for Godot scripting.

I'm pretty satisfied with Rider, but also interested in learning if Rider is the best choice.
So if you have solid experience in VS Code or Visual Studio to script Godot C#. Contact if you would rather use one of them.

## Code architecture and third-party libraries
Designing professional-grade software architectures often takes decades of experience, so my implementations may seem noob trying hard.
Please contact me if you have recommendations for improvement.

### Godot
Ciallo uses following 2D features of Godot: Rendering/Shader for stroke rendering, Input, GUI, Physics for click detection,
which basically cover every aspect of a 2D game.
So every piece of experience you have in 2D game development is helpful, and skills you learn from Ciallo can also be applied your future 2D game development.

### Component pattern
Ciallo heavily uses the [Arch](https://github.com/genaray/Arch) library for implementing component pattern in almost every piece of code.
Make sure you understand the component pattern [(tutorial)](https://gameprogrammingpatterns.com/component.html),
and the Arch library [documentation](https://arch-ecs.gitbook.io/arch).
> __Note__: Reading Arch document's first three tabs: Concepts, World and Entity is enough to begin with.
> Ciallo uses Arch for writing clean code, but not for CPU-cache optimizations (the "S" part of ECS).

See `WorldManager` class. Each user document is stored and managed by a `World` object.
Each `World` object create a "singleton entity" that stores "document-level singletons" data,
such as `DocumentSetting` for canvas parameters, `LayerTreeManager` for layer data, `CommandManager` for undoRedo stack, etc.
They should be one per document, so I call them "document-level singletons" and simply name the singleton entity variable as `Document`.

The current active `Document` is globally accessible, so does those document-level singletons.
You will see code like `var tree = Document.Get<LayerTreeManager>()` to visit the document's layer tree within the Tool or Command system code.

<details>
<summary>Why globalize the Document entity?</summary>
Though using global variables/singletons is commonly considered a bad practice, it's necessary for Ciallo.
Ciallo is an interactive graphics program, the interaction between subsystems is necessary by business.
As the business grows, it's impossible to predefine the accessibility scope of each subsystem.
So I think this design is reasonable.
</details>

## Code style
See my [instruction](../Ciallo/.github/copilot-instructions.md) to copilot.
