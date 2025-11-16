# Roadmap

## v0.1 EA plan

V0.1EA focus on showing off Ciallo techniques and developing MVP (minimum viable product) to support flat color drawings. Below are the features to develop, should be as feature-rich as MS paint.

- [x] Document/world manager
- [x] Export to Godot
- [x] Export to raster image
- [x] .Ciallo project file
- [x] Command undo/redo system
- [x] Property undo/redo
  - [ ] Document brushes properties undo/redo
- [x] Tool system infrastructure
- [x] Brush tool
  - [ ] More brushes
  - [x] More brush parameters
  - [x] Paint stabilizer
- [x] Paint fill tool
- [ ] Vector fill tool
  - [x] CGAL C++ code
- [x] Selection/transform tool
  - [x] Rect transform
  - [ ] Multi select and transform
  - [ ] Line binding system (Bézier curve only)
    - [x] Bézier curve geometry
  - [x] Polyline overlay rendering
- [x] Layer system
  - [x] Add, delete
  - [x] Rename
  - [x] Reorder
  - [x] Revert showing order
  - [ ] Merge
- [x] Import image as a layer
- [ ] Localization
  - [x] Infrastructure (ai translation)

## v0.2 EA plan

V0.2EA focus on supporting semi-painterly style for producing galgame illustrations (Tachie first, CG if possible).
Plan to follow [pikat](https://www.youtube.com/@pikat)'s feature list:

![](/.github/PikatFeatureList.png)

- [ ] Lasso tool like lasso on CSP vector layer
- [ ] Sculpt(liquify) tool like GP
- Layer
  - [ ] Basic modifiers
  - [ ] Folder
  - [ ] Blend modes
  - [ ] Lock & rasterize
  - [ ] Mask
- [ ] Technical stuffs

Aim to be able to produce business-level galgame tachies. Notify me if there are missing features to produce tachies in following style.

![](/.github/Ririko.png)

## v1.0 plan

Ciallo is largely inspired by Blender Grease Pencil (GP) 3D stroke.
Before release v1.0, Ciallo will have 2D copies of every GP's major features.

Beside GP, here is a rough unique feature list:

- Animation system similar to Clip Studio Paint (CSP)
  - Vector fill integration in depth
- Lasso tool identical to Photoshop or CSP's for raster image.
- Polygon gaps detection system (built with 2D game navigation system)
- Anime style lighting system integrated with Godot's 2D light (need research)
- Feature-rich GPU brush engine near to [Krita](https://krita.org/en/) and [MyPaint](https://www.mypaint.app/en/) (need research)

# Ciallo Contributing Guide

## Introduction

This guide is not yet complete.
The current version seems like Shen's personal book note, but it aims to be a comprehensive guide for developing Ciallo.

After getting a basic idea on Ciallo's code architecture here, you can check AI wiki [deepwiki](https://deepwiki.com/ShenCiao/Ciallo) to get in-depth details on how each system is implemented.

## Basic setup

### How to build

Ciallo is built on Godot. Building the core part of Ciallo is the same as building a standard Godot C# project:

- Set up Godot 4.5.1 with .Net9. You can follow an arbitrary [video guide](https://www.youtube.com/watch?v=7nExKQn1CAw), but pay attention to the version.
- Open the `Ciallo/project.godot` file with your Godot editor, then build and run.
  - Note: Godot will raise annoying errors about autoload before first build, we can safely ignore them.
- Enable the "Embedded game size stretches..." option in the game run window.

![](/.github/EnableStretch.png)

### IDE

In theory, you can use any IDE supporting C#. Follow the Godot [guide](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html#configuring-an-external-editor) to link the Godot editor with your IDE.

However, I suggest using JetBrains [Rider](https://www.jetbrains.com/rider/), which is free since 2024 and offers comprehensive productivity support for Godot scripting.

I'm pretty satisfied with Rider, but also interested in learning if Rider is the best choice.
So if you also have solid experience in scripting Godot C# with VS Code or Visual Studio, tell me your comparison.

## Code architecture and third-party libraries

Designing professional-level software architectures often takes decades of experience, so my implementations may seem noob trying hard.
Please contact me if you have suggestions for improvement.

### Godot 2D

Ciallo uses the vast majority of Godot features for developing a 2D game, and heavily uses nearly all types of GUI control nodes.
So every piece of experience you have in 2D game development is helpful, and skills you learn from Ciallo can also be applied your future game development.

You can find all the ui scenes and custom gui nodes in the `NodeControl` or `Widget` folders. (I should have renamed "NodeControl" as "GuiControl" or something else better).

### Component pattern and Frent library

Ciallo heavily uses the [frent](https://github.com/itsBuggingMe/Frent) library for realizing component pattern in almost every piece of code.
Make sure you understand the component pattern theory [(tutorial)](https://gameprogrammingpatterns.com/component.html),
and the first page of the frent library [documentation](https://itsbuggingme.github.io/Frent/docs/ecf.html).

I assume you already know about Entity and Component. In Ciallo, for those editable objects like strokes, layers, we create an entity for each object and add necessary components to the entity.
e.g. add `PolylineGeometry` compoent and `StrokeBrush` compoent to a stroke entity, `PolylineLayerSetting` component to a Polyline layer entity. You can find examples in `Ciallo/Command/New*Cmd.cs` files.

Also see the `AppWorldManager` class. Each user document is stored and managed by a `World` object.
Each `World` object creates an entity that stores "document-level singletons" data.
e.g. `DocumentSetting` for canvas settings, `LayerTreeManager` for layer data, `CommandManager` for undo redo stack, etc.

These data should be one per document, so I call them "document-level singletons" and name the entity as `Document`.
You can find self-explanatory code like `Document.Get<LayerTreeManager>()` to visit the document's layer tree.

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

<!--
<details>
<summary>Why globalize the Document entity?</summary>
Though using global variables/singletons is commonly considered a bad practice, it's necessary for Ciallo.
Ciallo is an interactive graphics program, the interaction between subsystems is necessary by business.
As the business grows, it's impossible to predefine the accessibility scope of each subsystem.
So I think this design is reasonable.
</details>
-->

### Two-way binding and R3 library

Ciallo heavily uses [R3](https://github.com/Cysharp/R3) library's `ReactiveProperty` implement two-way binding between data and UI.
You can find code like `colorButton.BindColor(ReactiveProperty<Color> color)` in UI code to intimate WPF's xaml binding behavior.

R3's document is not written for beginners. I put a lot of effort only to take a very basic grasp. Luckily, you don't have to learn too much about R3/ReactiveProgramming to start.
Just google for what is ReactiveProperty, two-way binding, or MVVM pattern.
Then you understand most of the R3 usage in Ciallo.

If you have to understand how I handle dragging mouse input with R3 (reactive programming) in the Layers panel, here is my learning path:

1. Know [reactive programming](https://gist.github.com/staltz/868e7e9bc2a7b8c1f754) concept first.
2. Then read [UniRx](https://github.com/neuecc/UniRx) to know the former version of R3.
3. Reference [ReactiveX operator document](https://reactivex.io/documentation/operators.html) to choose suitable operators.
4. Make hard guess on the problems to solve.

### MVP pattern

For those elements (strokes, polygons) visible on the canvas. They hold complex data not suitable for two-way binding.
Ciallo separates related code into the Data(Model), Rendering(View) and Command(Presenter).
You can find corresponding folders in the project directory.

The architecture can be explained by the [MVP pattern](https://www.geeksforgeeks.org/android/mvp-model-view-presenter-architecture-pattern-in-android-with-example/).
The command objects manage both data and view.
As the "Command" name suggests, it also implements the undo/redo system.

Rendering folder has `*View` node types to render actual objects.
Command folder has `*Cmd` types inheriting from `CommandBase` and implementing `Do`, `Undo` methods. The `CommandBase` internally utilizes Godot [`UndoRedo` object](https://docs.godotengine.org/en/stable/classes/class_undoredo.html).

### Data serialization and MessagePack

Data uses [MessagePack](https://github.com/MessagePack-CSharp/MessagePack-CSharp) library with C# built-in [DataContract] to serialize data into `.ciallo` file.
No DTO by design.
The mechanism is combined with the component system:

- When an entity is tagged with `ToSerializeTag`, it will be serialized to .Ciallo files.
- When its components are attributed with `[ToSerialize]` (and also [DataContract] when necessary). these components are serialized.
- An entity without `ToSerializeTag` but has `[ToSerialize]` component won't be serialized, including entity itself and its components.

## Code style

See my [instruction](../Ciallo/.github/copilot-instructions.md) to copilot.
