using System;
using Godot;

namespace Ciallo;

public static class RandomExtension
{
    public static Vector2 NextVector2(this Random random) => new(random.NextSingle(), random.NextSingle());
}