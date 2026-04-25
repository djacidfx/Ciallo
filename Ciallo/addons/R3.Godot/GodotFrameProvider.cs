#nullable enable

using System;
using System.Runtime.CompilerServices;
using Godot;
using R3.Collections;

namespace R3;

internal enum PlayerLoopTiming
{
    Process,
    PhysicsProcess,
    BeforeProcess,
}

public class GodotFrameProvider : FrameProvider
{
    public static readonly GodotFrameProvider Process = new GodotFrameProvider(PlayerLoopTiming.Process);
    public static readonly GodotFrameProvider PhysicsProcess = new GodotFrameProvider(PlayerLoopTiming.PhysicsProcess);
    public static readonly GodotFrameProvider BeforeProcess = new GodotFrameProvider(PlayerLoopTiming.BeforeProcess);

    FreeListCore<IFrameRunnerWorkItem> list;
    readonly object gate = new object();

    PlayerLoopTiming PlayerLoopTiming { get; }

    internal StrongBox<double> Delta = default!; // set from Node before running process.

    internal GodotFrameProvider(PlayerLoopTiming playerLoopTiming)
    {
        this.PlayerLoopTiming = playerLoopTiming;
        this.list = new FreeListCore<IFrameRunnerWorkItem>(gate);
    }

    public override long GetFrameCount()
    {
        if (PlayerLoopTiming == PlayerLoopTiming.PhysicsProcess)
        {
            return (long)Engine.GetPhysicsFrames();
        }
        else
        {
            // Both Process and BeforeProcess share the same frame counter
            return (long)Engine.GetProcessFrames();
        }
    }

    public override void Register(IFrameRunnerWorkItem callback)
    {
        list.Add(callback, out _);
    }

    internal void Run(double _)
    {
        long frameCount = GetFrameCount();

        var span = list.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            ref readonly var item = ref span[i];
            if (item != null)
            {
                try
                {
                    if (!item.MoveNext(frameCount))
                    {
                        list.Remove(i);
                    }
                }
                catch (Exception ex)
                {
                    list.Remove(i);
                    try
                    {
                        ObservableSystem.GetUnhandledExceptionHandler().Invoke(ex);
                    }
                    catch { }
                }
            }
        }
    }
}