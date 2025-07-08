using System;
using Godot;

namespace Ciallo;

public static class RandomExtension
{
    public static Vector2 NextVector2(this Random random) => new(random.NextSingle(), random.NextSingle());
    public static Vector3 NextVector3(this Random random) => new(random.NextSingle(), random.NextSingle(), random.NextSingle());
    public static Vector2I NextVector2I(this Random random) => new(random.Next(), random.Next());
    public static Vector3I NextVector3I(this Random random) => new(random.Next(), random.Next(), random.Next());
}