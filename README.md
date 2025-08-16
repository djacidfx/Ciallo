# Ciallo ～(∠・ω< )⌒★!

Ciallo is an open-source vector graphics program for professional digital painting.

It aims to compete with traditional painting software like Photoshop and Clip Studio Paint, while offering the following unique features:

## Unique features
### Vectorized raster brushes

We have resolution-independent, Photoshop-like brushes. They are researched by the developer and run entirely on your GPU.

<image></image>

### Vector fill

Like a bucket fill but in vector form: the positions to fill are tracked with "fill markers", the shape $\odot$ in the image.

We can manipulate the positions, colors, or any other filling properties by editing these markers.

(Under development) We will be able to copy and paste these markers between animation frames to achieve consistent fills, greatly reducing manual work.”

### Export to game engines

We can export the 2D vector artworks into the Godot game engine (as a .scn/.tscn file), keeping the artwork's resolution independent.

Other game engines will be supported in the future.

## Other features
The following features may be in your favor:

### No AI

Ciallo uses a custom vector format that is not trained by any AI.

Unlike the big company, Ciallo will never silently change the Terms of Service for stealing your artworks/stocks to train AI.

We plan to build a sharing platform for your vector artworks while protecting them from unethical AI training.

In the invisible-far future, Ciallo may offer AI-powered features --- always designed for professional artists. 

### Line binding

### Infinite canvas

### Lasso tool on vector stroke (like CSP)

## Download (Free on all platforms)

Steam | Itch.io

A system can run mid-size indie 2D games is required.

System requirements:

- OS: Windows 10 or higher
- Memory: 8GB or more
- Graphics card: Minimum NV RTX 2060 or AMD RX 5600 XT.

The developer cannot afford to develop a macOS version. Buying a MacBook pro will cost him half years’ budget for staying alive. Please help him buy an MBP.

> Tech note: Minimum card tier may be higher than your expectation since Ciallo uses Godot forward+ Vulkan renderer. The cards before them may be unfriendly to the Vulkan.

## Credits
### Support our future
### Coding frameworks/libraries

- Godot C#: [Why godot?](#tech-faq)
- Arch: Unity-like entity component pattern (without CPU-cache optimizations). 
- CGAL: Complex geometry operations.
- R3: Signal Enhancement and reactive programming.
- GdUnit4: Unit test framework.
- MessagePack: Binary serialization.
- Newtonsoft.Json: JSON serialization.
- Godot Source Generator: Godot auxiliary.

## Build Guide

Ciallo is built upon Godot. Building the core part of Ciallo is exactly the same as building a regular Godot C# project:

- Set up Godot 4.4.1 with .Net9. You can follow an arbitrary [video guide](https://www.youtube.com/watch?v=7nExKQn1CAw). Be mindful of the version. 

- Open the `Ciallo/project.godot` file with your Godot editor, then build and run.

## Development philosophy and roadmap

Ciallo is in an early stage of development, its version number will label with EA (early access).

For now, users still need a lot of features from the traditional painting software.
Throughout Early Access, we will research and develop core painting features using shaders and GPU APIs.

After finishing the EA versions, Ciallo will focus on developing peripheral systems to create 2D illustrations, animations for 2D game assets or textures in 3D games.

## Tech FAQ

Shen answers the following questions:

### Why develop on a game engine

**TLDR**: I cannot handle the complexity of modern graphics api.

My first attempt to develop Ciallo was with Vulkan, [project link](https://github.com/ShenCiao/CialloVulkan), motivated by the rather naïve reason: “my next-generation technology should be built on next-generation technical foundations.”

I soon realize the Vulkan’s complexity: it isn’t designed for one-person projects, and even highly experienced graphics engineers can make simple-yet-catastrophic mistakes, see [Cherno’s Vulkan story](https://youtu.be/bUUZ1iD9_e4?si=vVCUxXU-dScgcZx5&t=1438). Only large teams can truly afford the mental burden of Vulkan.

So, I decided to sacrifice the freedom on controlling graphics in exchange for the productivity as much as I can. Under this idea, one of the game engines is the best choice.

### Why Godot, not Unity or Unreal

Godot has the best support for 2D rendering and `ColorPicker` control. 

E.g., As of June 2025, Godot is the only engine that supports rendering a polygon directly from point data.
Moreover, Godot’s ColorPicker control convinced me to choose it over Unity.

BTW, I hope Godot can make `EditorSpinSlider` runtime available, see [issue](https://github.com/godotengine/godot-proposals/issues/3244#issuecomment-911489983).