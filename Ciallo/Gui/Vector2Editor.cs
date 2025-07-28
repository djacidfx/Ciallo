using Godot;
using System;

[GlobalClass, Tool]
public partial class Vector2Editor : HBoxContainer
{
    [Signal] 
    public delegate void ValueChangedEventHandler(Vector2 newValue);
    
    public SpinBox SpinX { get; private set; }
    public SpinBox SpinY { get; private set; }
    
    [Export] public double MaxValue
    {
        get => SpinX.MaxValue;
        set
        {
            SpinX.MaxValue = value;
            SpinY.MaxValue = value;
        }
    }
    
    [Export] public double MinValue
    {
        get => SpinX.MinValue;
        set
        {
            SpinX.MinValue = value;
            SpinY.MinValue = value;
        }
    }
    
    [Export] public Vector2 Value
    {
        get => new((float)SpinX.Value, (float)SpinY.Value);
        set
        {
            SpinX.Value = value.X;
            SpinY.Value = value.Y;
        }
    }
    
    [Export] public double Step
    {
        get => SpinX.Step;
        set
        {
            SpinX.Step = value;
            SpinY.Step = value;
        }
    }

    [Export] public bool ExpEdit
    {
        get => SpinX.ExpEdit;
        set
        {
            SpinX.ExpEdit = value;
            SpinY.ExpEdit = value;
        }
    }

    [Export] public bool Rounded
    {
        get => SpinX.Rounded;
        set
        {
            SpinX.Rounded = value;
            SpinY.Rounded = value;
        }
    }

    public Vector2Editor()
    {
        SpinX = new SpinBox();
        SpinY = new SpinBox();
        AddChild(SpinX);
        AddChild(SpinY);
        
        SpinX.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        SpinY.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        
        ConfigureSpin(SpinX, 0);
        ConfigureSpin(SpinY, 1);
    }
    
    private void ConfigureSpin(SpinBox spin, int component)
    {
        spin.AllowGreater = false;
        spin.AllowLesser = false;
        spin.Alignment = HorizontalAlignment.Left;

        // Connect to our handler, passing which component this spinbox represents
        spin.ValueChanged += rawValue => OnSpinValueChanged(rawValue, component);
    }

    public override void _ExitTree()
    {
        SpinX.QueueFree();
        SpinY.QueueFree();
    }

    private void OnSpinValueChanged(double rawValue, int component)
    {
        EmitSignal(SignalName.ValueChanged, Value);
    }
}