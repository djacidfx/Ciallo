# Roadmap

# Ciallo Contributing Guide

## Introduction

This guide is not yet complete.
The current version is a note for AI to read, but it aims to be a comprehensive guide for humans interested in developing Ciallo.

After getting a basic idea on Ciallo's code architecture here, you can check AI wiki [deepwiki](https://deepwiki.com/ShenCiao/Ciallo) to get in-depth details on how each system is implemented.

## Basic setup

### How to build

Ciallo is built on Godot. Building the core part of Ciallo is the same as building a standard Godot C# project:

- Set up Godot 4.5.2 with .Net10. You can follow an arbitrary [video guide](https://www.youtube.com/watch?v=7nExKQn1CAw), but pay attention to the version.
- Open the `Ciallo/project.godot` file with your Godot editor, then build and run.
  - Note: Godot will raise annoying errors about autoload before first build, we can safely ignore them.
- Enable the "Embedded game size stretches..." option in the game run window.

![](/.github/EnableStretch.png)

## Code architecture and third-party libraries

Designing professional-level software architectures often takes decades of experience, so my implementations may seem noob trying hard.
Please contact me if you have suggestions for improvement.

### Godot 2D

Ciallo uses the vast majority of Godot features for developing a 2D game, and heavily uses nearly all types of GUI control nodes.
So every piece of experience you have in 2D game development is helpful, and skills you learn from Ciallo can also be applied your future game development.

You can find all the ui scenes and custom gui nodes in the `GuiControl` or `Widget` folders.

### Component pattern and Frent library

Ciallo heavily uses the [frent](https://github.com/itsBuggingMe/Frent) library for realizing component pattern in almost every piece of code.
Make sure you understand the component pattern theory [(tutorial)](https://gameprogrammingpatterns.com/component.html),
and the first page of the frent library [documentation](https://itsbuggingme.github.io/Frent/docs/ecf.html).

I assume you already know about Entity and Component. In Ciallo, for those editable objects like strokes, layers, we create an entity for each object and add necessary components to the entity.
e.g. add `PolylineGeometry` compoent and `StrokeSetting` compoent to a stroke entity, `PolylineLayerSetting` component to a Polyline layer entity. You can find examples in `Ciallo/Command/New*Cmd.cs` files.

Also see the `AppDocumentManager` class. Each user document is stored and managed by a `World` object.
Each `World` object creates an entity that stores "document-level singletons" data.
e.g. `DocumentSetting` for canvas settings, `LayerTreeNode` for layer node, `CommandManager` for undo redo stack, etc.

These data should be one per document, so I call them "document-level singletons" and name the entity as `Document`.
You can find code like `Document.Get<LayerTreeNode>()` to visit the document's layer tree root.

<details>
<summary>Why using an ECS library?</summary>

When I developed my research project, I found Inkscape and Krita both use an integer id value to manage editable objects.
So to imitate them, I tried to find a 3rd party library can do the two things:

- Generate unique ids.
- Manage objects lifecycle with these id values.

An ECS library is a very nice fit after ignoring the "s" part (cache-friendly system coding).
In fact, if you search C# libraries supporting component pattern, those ECS libraries value too much in cache-friendly performance, which is unnecessary to Ciallo.
Frent is the only C# ECS library that values more about code architecture design rather than performance.

I started using EnTT for my C++ project (undoubtedly overdesigned in that project).
Later I have learned more about software architecture and realised probably I happened to make a nice choice.
Check [this](https://youtu.be/wo84LFzx5nI?si=YPJa9tF5mult5ulA&t=3987) lecture OOP programming history which mentions Sketchpad.
Ciallo as a descendant of Sketchpad may has the same sense of good code design.

</details>

### Two-way binding and R3 library

Ciallo heavily uses [R3](https://github.com/Cysharp/R3) library's `ReactiveProperty` to replace traditional reflection and implement two-way binding between data and UI.
You can find code like `colorButton.BindColor(ReactiveProperty<Color> color)` in UI code to intimate WPF's xaml binding behavior.

R3's document is not written for beginners. I put a lot of effort only to take a very basic grasp. Luckily, you don't have to learn too much about R3/ReactiveProgramming to start.
Just google for what is ReactiveProperty, two-way binding, or MVVM pattern.
Then you understand most of the R3 usage in Ciallo.

If you have to understand how I handle dragging mouse input with R3 (reactive programming) in the Layers panel, here is my learning path:

1. Know [reactive programming](https://gist.github.com/staltz/868e7e9bc2a7b8c1f754) concept first.
2. Then read [UniRx](https://github.com/neuecc/UniRx) to know the former version of R3.
3. Reference [ReactiveX operator document](https://reactivex.io/documentation/operators.html) to choose suitable operators.
4. Make hard guess on the problems to solve.

### Data serialization and DuckDB

Project data is stored in a DuckDB-backed `.ciallo` file.
No DTO by design.
The mechanism is combined with the component system:

- When an entity is tagged with `ToSerializeTag`, it will be persisted to `.ciallo` files.
- Components attributed with `[ToSerialize]` are mapped to DuckDB columns. Scalar values use DuckDB scalar types, and creative value types such as `Color`, `Vector2`, `Transform2D`, and Bezier data use DuckDB `STRUCT`, `LIST`, or array-like structured types when possible.
- An entity without `ToSerializeTag` but with `[ToSerialize]` components won't be persisted, including the entity itself and its components.
