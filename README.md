# Ciallo ～(∠・ω< )⌒★!

Ciallo is an open-source graphics program for professional digital painting.

It aims to compete with traditional painting software like Photoshop and Clip Studio Paint, while offering the following unique features:

## Unique features
### Vectorized raster brushes

Resolution-independent, Photoshop-like brushes: The brushes are entirely drawn on your graphics card, and the techniques behind are invented by the developer (Shen Ciao).

### Vector fill

Bucket fill in vector form: The positions to fill are tracked with "fill markers", the shape $\odot$ in the image.

We can manipulate the positions, colors, or any other filling properties by editing these markers.

(Under development) We will be able to copy and paste these markers between animation frames, greatly reducing manual work.”

### Export to game engines

Export your artwork into the Godot game engine (as a .scn/.tscn file), and it keeps resolution independent.

Ciallo also export orignal layers to facilitate creating CG variations.

Other game engines will be supported in the future.

## Other features
The following features may be in your favor:

### No generative AI

Ciallo uses a custom vector format that is not trained by any AI.

Unlike the big [company](https://www.youtube.com/watch?v=DoM3nUD-1Ro), Ciallo will never change the terms of service silently to steal your artwork stocks for training AI.
We plan to build a sharing platform while protecting your artworks from unethical AI training.

In the invisible-far future, Ciallo may offer AI-powered features --- but always designed for professional artists.

### Line binding

### Infinite canvas

### Lasso tool on vector stroke (like CSP)

## Download (Free on all platforms)

Steam | Itch.io

System requirements (Require a system can run large-size 2D games.):

- OS: Windows 10 or higher
- Memory: 8GB or more
- Graphics card: Minimum NV GTX 1060 or AMD Radeon RX 480.

> About MacOS, Linux: The developer literally wishes but cannot afford to develop a macOS version. Buying a MacBook Pro will cost him half a year’s living budget. Consider patreon him for a macOS version.

## Development philosophy and roadmap

Ciallo is in an early stage of development; its version number will be labeled as EA (early access).

During the EA stage, we mainly research and develop traditional paint software features with modern shaders and GPU APIs. After finishing those core paint features, we will open Steam Workshop, marking the end of the EA stage.

After finishing the EA stage, Ciallo will focus on peripheral systems to create 2D game assets, including illustrations, 2D animations and hand-drawn textures in 3D.

#### Feature requests

The developer basically knows the most needed features during the EA stage. These are the YouTube channels he learns painting: [Dong Chang](https://www.youtube.com/@DongChang) | [Aaron's Painter Tutorials](https://www.youtube.com/@AaronsPainterTutorials) | [saitonaoki](https://www.youtube.com/@saitonaoki2) | [Oridays](https://www.youtube.com/@oridays).

If you eagerly need a feature to be deployed within a week/month, consider contacting the developer and sponsoring the project.

## Sponsor your future
Ciallo's mission is to bring you next-generation techniques for 2D hand-drawn art.
The developer Shen Ciao invents the techniques driven by the passion to Anime. It needs your support to keep them free and accessible to everyone.

Moreover, In this age of AI, we all face unprecedented challenges by the surging AI techniques.
Hope Ciallo will be the tool helping you shine in this era --- the tool liberating your creativity, not replacing it with AI.
Sponsor us to shape the future of your painting, and keep the creativity alive in the future AI-driven world.

## Credits
### Project name
The name "Ciallo" is a combination of the Italian "Ciao" and English "Hello", and comes from the galgames developed by [Yuzusoft](https://www.yuzu-soft.com/). We won't shame this name.

### Coding frameworks/libraries

- [Godot C#](https://godotengine.org/): [Why godot?](#tech-faq)
- [Arch](https://github.com/genaray/Arch): Unity-like (gameobject/entity) component pattern (no need for CPU-cache optimizations).
- [CGAL](https://www.cgal.org/): Complex geometry operations.
- [R3](https://github.com/Cysharp/R3): Signal enhancement and reactive programming.
- [GdUnit4](https://github.com/MikeSchulze/gdUnit4): Unit test framework.
- [MessagePack](https://github.com/MessagePack-CSharp/MessagePack-CSharp): Binary serialization.
- [Newtonsoft.Json](https://www.newtonsoft.com/json): JSON serialization.
- [GodotSharp.SourceGenerators](https://github.com/Cat-Lips/GodotSharp.SourceGenerators): Godot auxiliary.

## Build Guide
Ciallo is built on Godot. Building the core part of Ciallo is the same as building a standard Godot C# project:

- Set up Godot 4.4.1 with .Net9. You can follow an arbitrary [video guide](https://www.youtube.com/watch?v=7nExKQn1CAw), but pay attention to the version.
- Open the `Ciallo/project.godot` file with your Godot editor, then build and run.

Go to [Contributing Tab](https://github.com/ShenCiao/Ciallo?tab=contributing-ov-file#how-to-build) for a more complete guide.

## Tech FAQ

Shen answers the following questions:

### Why develop on a game engine?

**TL;DR**: I cannot handle the complexity of modern graphics APIs.

My first attempt to develop Ciallo was with Vulkan, [project link](https://github.com/ShenCiao/CialloVulkan), motivated by the rather naive reason: “my next-generation technology should be built on next-generation technical foundations.”

I soon realize the complexity of Vulkan: it isn’t designed for one-person projects, and even highly experienced graphics engineers can make simple yet catastrophic mistakes, see [Cherno’s Vulkan story](https://youtu.be/bUUZ1iD9_e4?si=vVCUxXU-dScgcZx5&t=1438). Only large teams can truly afford the mental burden of Vulkan.

So, I decided to sacrifice the freedom of controlling graphics in exchange for productivity. Under this idea, one of the game engines is the best choice.

You can imagine Ciallo as a building/RTS game, e.g., _City Skylines_, _Warcraft III_. Players build strokes (polylines with brush materials) and place color blocks (polygons with fill materials) in the game world (canvas).

### Why Godot, not Unity or Unreal?

Godot has the best 2D rendering infrastructure and GUI widgets. 

For example, as of June 2025, Godot is the only engine that supports rendering a filled polygon directly from a list of points (without manual tessellation).
Moreover, Godot’s `ColorPicker` control convinced me to choose it over Unity.

If we have to give up using Godot for whatever reasons someday, we may consider using [defold](https://defold.com/) or [flax](https://flaxengine.com/).

BTW, I hope Godot can make `EditorSpinSlider` runtime available, see [issue](https://github.com/godotengine/godot-proposals/issues/3244#issuecomment-911489983).

### Why not GDScript?
Dynamically-typed languages cannot support our project (or, IMHO, any projects that need custom types more than 10). C# is a better choice, also for its larger community.

But I do like the GDScript language itself. In my wet dream, Godot will discard GDScript for engine scripting and turn it into a high-level shading language for graphics --- Comparing to C#, GDScript lacks many features, but compared to glsl/hlsl, it's already considered feature-rich. The graphics community really lacks advanced programming languages for shading.
