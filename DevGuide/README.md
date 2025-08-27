# Ciallo development guide
## Introduction
This guide (and Ciallo's code) is not yet complete.
Although the current version seems like Shen's personal booknote, it aims to be a comprehensive guide for developing Ciallo.

## Basic setup
### How to build

Ciallo is built on Godot. Building the core part of Ciallo is the same as building a standard Godot C# project:

- Set up Godot 4.4.1 with .Net9. You can follow an arbitrary [video guide](https://www.youtube.com/watch?v=7nExKQn1CAw), but pay attention to the version.
- Open the `Ciallo/project.godot` file with your Godot editor, then build and run.

### IDE
In thoery, you can use any IDEs supporting C#. Follow the Godot's [guide](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html#configuring-an-external-editor) to link the Godot editor with your ide.

However, I suggest using Jetbrain [Rider](https://www.jetbrains.com/rider/) which is free since 2024 and offers comprehensive productivity supports for Godot cripting.

I'm pretty satisified with Rider, but I also interest in learning if Rider is the best choice.
So if you have solid experience in VS Code or Visual Studio to script Godot C#. Contact if you would rather to use one of them.

## Code architecture and third-party libraries
Designing professional-grade software architectures often takes decades of experience, so these implementations may seem noob trying hard.
Please contact if you have recommendations for improvement.
