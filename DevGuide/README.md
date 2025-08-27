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

However, I suggest using Jetbrain [Rider](https://www.jetbrains.com/rider/) which is free since 2024 and offers comprehensive supports for Godot scripting.

I'm pretty satisified with Rider, but I also interest in learning if Rider is the best choice.
So if you have deep experience in VS Code or Visual Studio to script Godot C#. Contact me if you would like to use one of them.

## Code architecture and 3rd party library

