#nullable enable

using System.Runtime.CompilerServices;
using Godot;

namespace R3;

public partial class FrameProviderDispatcher : Node
{
    StrongBox<double> processDelta = new StrongBox<double>();
    StrongBox<double> physicsProcessDelta = new StrongBox<double>();

    public override void _Ready()
    {
        GodotProviderInitializer.SetDefaultObservableSystem();

        ((GodotFrameProvider)GodotFrameProvider.Process).Delta = processDelta;
        ((GodotFrameProvider)GodotFrameProvider.PhysicsProcess).Delta = physicsProcessDelta;
        // BeforeProcess shares the same delta box as Process; delta is from the previous frame
        // when ProcessFrame fires, but that is acceptable since BeforeProcess is not used as a TimeProvider.
        ((GodotFrameProvider)GodotFrameProvider.BeforeProcess).Delta = processDelta;
        GetTree().ProcessFrame += OnProcessFrame;
    }

    public override void _ExitTree()
    {
        GetTree().ProcessFrame -= OnProcessFrame;
    }

    void OnProcessFrame()
    {
        ((GodotFrameProvider)GodotFrameProvider.BeforeProcess).Run(processDelta.Value);
    }

    public override void _Process(double delta)
    {
        processDelta.Value = delta;
        ((GodotTimeProvider)GodotTimeProvider.Process).time += delta;
        ((GodotFrameProvider)GodotFrameProvider.Process).Run(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        physicsProcessDelta.Value = delta;
        ((GodotTimeProvider)GodotTimeProvider.PhysicsProcess).time += delta;
        ((GodotFrameProvider)GodotFrameProvider.PhysicsProcess).Run(delta);
    }
}