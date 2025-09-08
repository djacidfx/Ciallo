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
    - [ ] Resize brush interactor
  - [ ] Paint fill tool
  - [ ] Vector fill tool
    - [x] CGAL C++ code
    - [ ] C# tool code
  - [ ] Selection/move tool
    - [ ] Line binding system (Bézier curve only)
      - [x] Bézier curve geometry
    - [ ] Design
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
  
![](./EnableStretch.png)

### IDE
In theory, you can use any IDE supporting C#. Follow the Godot [guide](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html#configuring-an-external-editor) to link the Godot editor with your IDE.

However, I suggest using JetBrains [Rider](https://www.jetbrains.com/rider/), which is free since 2024 and offers comprehensive productivity support for Godot scripting.

I'm pretty satisfied with Rider, but also interested in learning if Rider is the best choice.
So if you have solid experience in VS Code or Visual Studio to script Godot C#. Contact if you would rather use one of them.

## Code architecture and third-party libraries
Designing professional-grade software architectures often takes decades of experience, so my implementations may seem noob trying hard.
Please contact me if you have recommendations for improvement.

## Code style
See my [instruction](../Ciallo/.github/copilot-instructions.md) to copilot.
