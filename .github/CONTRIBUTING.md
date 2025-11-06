# Roadmap
## v0.1 EA plan 
To get a little bit more than a minimum viable product, below are the features for finishing the Demo version.
Will make a release on steam and start version "v0.1 EA" after finish these features.

- [x] Document/world manager
- [x] Export to Godot
- [x] Export to raster image
- [x] .Ciallo project file
- [x] Command undo/redo system
- [ ] Property undo/redo
- [x] Tool system infrastructure
- [x] Brush tool
  - [ ] More brushes
  - [x] More brush parameters
  - [x] Paint stabilizer
- [x] Paint fill tool
- [ ] Vector fill tool
  - [x] CGAL C++ code
- [x] Selection/move tool
  - [x] Rect transform
  - [ ] Line binding system (Bézier curve only)
    - [x] Bézier curve geometry
  - [x] Polyline overlay rendering
- [ ] Basic lasso tool
- [x] Layer system
  - [x] Add, delete
  - [x] Rename
  - [x] Reorder
  - [x] Revert showing order
  - [ ] Merge
- [x] Import image as a layer
- [ ] Localization
  - [x] Infrastructure (ai translation)


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
So if you have solid experience in VS Code or Visual Studio to script Godot C#. Contact if you would rather use one of them.

## Code architecture and third-party libraries
Designing professional-grade software architectures often takes decades of experience, so my implementations may seem noob trying hard.
Please contact me if you have recommendations for improvement.

After getting a basic idea on how to we those 3rd party libraries, you can check AI generated code wiki [deepwiki](https://deepwiki.com/ShenCiao/Ciallo) to get in depth ideas on how each system is implemented.

### Godot 2D

Ciallo uses the vast majority of Godot features for developing a 2D game, and heavily uses nearly all types of GUI control nodes.
So every piece of experience you have in 2D game development is helpful, and skills you learn from Ciallo can also be applied your future game development.

You can find all the ui scenes and custom gui nodes in the `NodeControl` or `Widget` folders. (I should have renamed "NodeControl" as "GuiControl" or something else better).

### Component pattern and Frent library
Ciallo heavily uses the [frent](https://github.com/itsBuggingMe/Frent) library for realizing component pattern in almost every piece of code.
Make sure you understand the component pattern theory [(tutorial)](https://gameprogrammingpatterns.com/component.html),
and the first page of the frent library [documentation](https://itsbuggingme.github.io/Frent/docs/ecf.html).

See the `AppWorldManager` class. Each user document is stored and managed by a `World` object.
Each `World` object creates an entity that stores "document-level singletons" data.
e.g. `DocumentSetting` for canvas settings, `LayerTreeManager` for layer data, `CommandManager` for undo redo stack, etc.

These data should be one per document, so I call them "document-level singletons" and name the entity as `Document`.
You can find self-explanatory code like `Document.Get<LayerTreeManager>()` to visit the document's layer tree.

For those editable object like strokes, layers, we will create entities and add components such as `PolylineGeometry`, `StrokeBrush`, `PolylineLayerSetting` object to the entities.

<details>
<summary>Why using an ECS library?</summary>

When I developed my research project, I found Inkscape and Krita both use an integer id value to manage editable objects.
So to imitate them, I tried to find a 3rd party library can do the two things:

- Generate unique ids.
- Manage objects lifecycle with these id values.

An ECS library is a very nice fit after ignoring the "s" part (cache-friendly system coding).
In fact, if you search for a 3rd party library for component pattern, an ECS library is the only choice. There are no dedicate libraries for component pattern.

I used EnTT for my C++ project (undoubtedly overdesigned in that project).
And when I started C#, I searched for a C# ECS library similar to EnTT, tried Arch and Massive then switch to Frent.

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
