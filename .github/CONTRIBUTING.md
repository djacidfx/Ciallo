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


## Release and tags

Ciallo uses `dev` for daily integration and `main` for formal releases.
Create feature branches from `dev` using `<your-name>/<feature-topic>`, for example `alice/brush-preview`.
There are no alpha/beta tags and no release branches for now.

Publishing is done by running a workflow from the Actions page, not by pushing tags
by hand. Each publish workflow reads the app version from `Ciallo/project.godot`
(`config/version`), creates the tag for you, and dispatches the shared Ciallo Release
pipeline on it. A hand-pushed `v*` tag does nothing. The pipeline also requires the C#
project to use a `Godot.NET.Sdk/*-ciallo.g<sha>` package already published to the public
custom Godot NuGet feed.

Before publishing an RC or final release, update `Ciallo/project.godot` and push it to
the branch you will publish (`dev` for RC, `main` for release):

```ini
config/version="0.0.2"
```

The publish workflow tags `v<config/version>`, so the version in `project.godot` is the
single source of truth — you never type the version anywhere else.
After a final release, bump `dev` to the next planned version immediately, while `main`
stays on the released version.

### Feature test builds

Use a feature test build when a branch needs downloadable builds for testers before
merging to `dev`. It produces short-lived GitHub Actions artifacts only — no GitHub
Release, no Steam upload.

Run the **Ciallo Publish Feature Test** workflow from the Actions page and pick the
branch to publish in the "Run workflow" ref dropdown. It publishes that branch's HEAD,
derives a slug from the branch name, and tags `ft/<slug>.<N>` with `<N>` auto-incremented
(for example branch `alice/brush-preview` -> `ft/alice-brush-preview.1`). The branch name
is not validated; whatever branch you dispatch is what gets built.

### Release candidates

Run the **Ciallo Publish RC** workflow from the Actions page. Anyone with write access
can run it. It publishes the current tip of `origin/dev`, auto-increments the RC counter
from existing tags, and tags `v<config/version>-rc.<N>`.

RC builds create GitHub Prereleases with Windows, Linux, and macOS artifacts, and are
uploaded to Steam and set live on the **development** branch automatically for testers.

### Final releases (managed by owner)


Then run the **Ciallo Publish Release** workflow from the Actions page. The run pauses for
a required reviewer (project owner) in the `production` environment before anything is
tagged — anyone can start it, but only an approver can complete it. After approval it
publishes the tip of `origin/main` and tags `v<config/version>`.

Final builds create normal GitHub Releases and are uploaded to Steam's **default** branch,
but are NOT set live automatically — a project owner must promote the build by hand in
Steamworks App Admin > Builds. Re-running the workflow for a version whose tag already
exists overwrites it (the tag is force-moved, the GitHub Release and Steam build are
replaced), so a failed publish can simply be re-run; bumping `config/version` remains the
norm for an actual new release.
After the final release is verified, old RC prereleases can be deleted manually when they
are no longer useful.

### Hotfix releases

Hotfix releases aim to publish bug fix within 5-10min. 
Use a hotfix when the latest stable release needs an urgent public fix. 
Hotfixes are temporary prereleases under the already published stable version:

```text
v0.1.0
v0.1.0-hotfix.1
v0.1.0-hotfix.2
v0.1.1
```

Hotfixes are allowed only for the latest stable GitHub Release.

Create hotfix branches from `main` using the `hotfix/` prefix:

```bash
git checkout main
git pull --ff-only
git checkout -b hotfix/fix-export-crash
```

Open a PR from `hotfix/fix-export-crash` to `main`.
Hotfix PRs must be squash merged.
After the PR is merged, the Ciallo Hotfix Release workflow automatically:

- creates the next `vX.Y.Z-hotfix.N` tag on the merged `main` commit;
- dispatches the Ciallo Release pipeline for that tag;
- publishes the tag as a GitHub Prerelease with exported Windows, Linux, and macOS artifacts;
- creates a backport PR from `backport/vX.Y.Z-hotfix.N-to-dev` into `dev`.

If the automatic cherry-pick to `dev` conflicts, the workflow fails and the fix must be backported manually.
Do not manually bump `project.godot` for hotfixes; bump it only for RCs, final releases, and the next accumulated stable patch release.
