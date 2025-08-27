# Ciallo ～(∠・ω< )⌒★!

Ciallo is an open-source vector graphics program for professional digital painting.

It aims to compete with traditional painting software like Photoshop and Clip Studio Paint, while offering the following unique features:

## Unique features
### Vectorized raster brushes

We have resolution-independent, Photoshop-like brushes. They are researched by the developer (Shen Ciao) and run entirely on your GPU.

### Vector fill

Like a bucket fill but in vector form: the positions to fill are tracked with "fill markers", the shape $\odot$ in the image.

We can manipulate the positions, colors, or any other filling properties by editing these markers.

(Under development) We will be able to copy and paste these markers between animation frames, greatly reducing manual work.”

### Export to game engines

We can export the 2D vector artworks into the Godot game engine (as a .scn/.tscn file), keeping the artwork's resolution independent.

Other game engines will be supported in the future.

## Other features
The following features may fit your favor:

### No generative AI

Ciallo uses a custom vector format that is not trained by any AI.

Unlike the big company, Ciallo will never change the term of service silently to steal your artwork stocks for [training AI](https://www.youtube.com/watch?v=DoM3nUD-1Ro).
We plan to build a sharing platform for your vector artworks while protecting them from unethical AI training.

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

> About MacOS, Linux: The developer literally wish but cannot afford developing a macOS version. Buying a MacBook pro will cost him half years’ budget for staying alive. Please help him buy an MBP.

## Development philosophy and roadmap

Ciallo is in an early stage of development, its version number will label with EA (early access).

During the EA stage, we mainly research and develop traditional paint software features with modern shaders and GPU APIs. After finishing those core paint features, we will open Steam Workshop, labeling the end of EA stage.

After finishing the EA stage, Ciallo will focus on peripheral systems to create 2D game assets, including illustrations, 2D animations and hand-drawn textures in 3D.

#### Feature requests

The developer basically knows those most needed features in the EA stage. These are the YouTube channels he learns painting: [Dong Chang](https://www.youtube.com/@DongChang) | [Aaron's Painter Tutorials](https://www.youtube.com/@AaronsPainterTutorials) | [saitonaoki](https://www.youtube.com/@saitonaoki2) | [Oridays](https://www.youtube.com/@oridays).

If you eagerly need a feature to be deployed a week/month, consider contact the developer and sponsor the project.

## Sponsor your future
Ciallo's unique techniques are invented by the developer, aiming to be a new standard in digital painting. These techniques are born with a passion to 2D animes, and needs your sponsor to sustain free for everyone.

Moreover, in this AI era, all human careers are being challenged by the surging AI techniques. Hope Ciallo will be the tool to liberate your creativity, not replace it. Help us shape the next chapter of digital painting technique, and keep the creativity alive in the future AI-driven world.

## Credits
### Project name
The name "Ciallo" is the combination of the Italian "Ciao" and English "Hello", comes from the galgames developed by [Yuzusoft](https://www.yuzu-soft.com/). We won't shame on this name.

### Coding frameworks/libraries

- [Godot C#](https://godotengine.org/): [Why godot?](#tech-faq)
- [Arch](https://github.com/genaray/Arch): Unity-like (gameobject/entity) component pattern (no need for CPU-cache optimizations).
- [CGAL](https://www.cgal.org/): Complex geometry operations.
- [R3](https://github.com/Cysharp/R3): Signal Enhancement and reactive programming.
- [GdUnit4](https://github.com/MikeSchulze/gdUnit4): Unit test framework.
- [MessagePack](https://github.com/MessagePack-CSharp/MessagePack-CSharp): Binary serialization.
- [Newtonsoft.Json](https://www.newtonsoft.com/json): JSON serialization.
- [GodotSharp.SourceGenerators](https://github.com/Cat-Lips/GodotSharp.SourceGenerators): Godot auxiliary.

## Build Guide
Go to [dev guide](./DevGuide) for a more comprehensive version.

Ciallo is built on Godot. Building the core part of Ciallo is the same as building a standard Godot C# project:

- Set up Godot 4.4.1 with .Net9. You can follow an arbitrary [video guide](https://www.youtube.com/watch?v=7nExKQn1CAw), but pay attention to the version.
- Open the `Ciallo/project.godot` file with your Godot editor, then build and run.

## Tech FAQ

Shen answers the following questions:

### Why develop on a game engine?

**TL;DR**: I cannot handle the complexity of modern graphics apis. Let game engines handle it.

My first attempt to develop Ciallo was with Vulkan, [project link](https://github.com/ShenCiao/CialloVulkan), motivated by the rather naive reason: “my next-generation technology should be built on next-generation technical foundations.”

I soon realize the Vulkan’s complexity: it isn’t designed for one-person projects, and even highly experienced graphics engineers can make simple-yet-catastrophic mistakes, see [Cherno’s Vulkan story](https://youtu.be/bUUZ1iD9_e4?si=vVCUxXU-dScgcZx5&t=1438). Only large teams can truly afford the mental burden of Vulkan.

So, I decided to sacrifice the freedom on controlling graphics in exchange for the productivity as much as I can. Under this idea, one of the game engines is the best choice.
You can imagine Ciallo as a building/RTS game, e.g. _City Skylines_, _Warcraft_ III, players build strokes and place color blocks in the game world (canvas).

### Why Godot, not Unity or Unreal?

Godot has the best 2D rendering and UI controls. 

E.g., As of June 2025, Godot is the only engine that supports rendering a filled polygon directly from a list of points (without manual tessellation).
Moreover, Godot’s `ColorPicker` control convinced me to choose it over Unity.

BTW, I hope Godot can make `EditorSpinSlider` runtime available, see [issue](https://github.com/godotengine/godot-proposals/issues/3244#issuecomment-911489983).

### Why not gdscript?
Dynamically-typed languages cannot support our project (or IMHO, any projects need custom types more than 10). C# is a better choice, also for its larger community.

But I do like the gdscript language itself. In my wet dream, Godot will discard gdscript for engine scripting and turn it into a high-level shading language for graphics --- Comparing to C#, gdscript lacks a lot of features, but to glsl/hlsl, it's already been crazyly feature-riched. The graphics community really lacks of advanced programming lanuages for shading.