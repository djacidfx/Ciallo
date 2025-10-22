using System;
using Frent;
using Godot;

namespace R3;

public static class GodotNodeExtensions
{
    /// <summary>
    /// Dispose self on target node has bee tree exited.
    /// </summary>
    /// <param name="disposable"></param>
    /// <param name="node"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns>Self disposable</returns>
    /// <remarks>
    /// Godot doesn't expose "node destroyed" signal, so have to use "tree exited" signal here, very ridiculous.
    /// </remarks>
    public static T AddTo<T>(this T disposable, Node node) where T : IDisposable
    {
        // Shen note: Decide to remove the "Node must inside the tree" constraints here.
        // Since it's damaging my brain to track the node whether inside tree or not.
        // Always be mindful.

        // oringal code:

        // // Note: Dispose when tree exited, so if node is not inside tree, dispose immediately.
        // if (!node.IsInsideTree()) 
        // {
        //     if (!node.IsNodeReady()) // Before enter tree
        //     {
        //         GD.PrintErr("AddTo does not support to use before enter tree.");
        //     }
        //
        //     disposable.Dispose();
        //     return disposable;
        // }

        node.TreeExited += () => disposable.Dispose();
        return disposable;
    }

    /// <remarks>
    /// Godot doesn't expose "node destroyed" signal. Use Entity's OnDelete event can cover the most usages.
    /// </remarks>
    public static T AddTo<T>(this T disposable, Entity e) where T : IDisposable
    {
        e.OnDelete += _ => disposable.Dispose();
        return disposable;
    }
}