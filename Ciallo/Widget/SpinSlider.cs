using System;
using Ciallo.Misc;
using Godot;
using R3;

namespace Ciallo.Widget;

/// <summary>
/// This spin slider aims to be same as Godot's EditorSpinSlider. Current version is visually different.
/// </summary>
[GlobalClass, Tool]
public partial class SpinSlider : HBoxContainer
{
    [Signal]
    public delegate void ValueChangedEventHandler(double newValue);
    
    public HSlider Slider { get; private set; }
    public SpinBox SpinBox { get; private set; }
    
    #region Export
    
    private double _minValue = 0.0;
    [Export] public double MinValue
    {
        get => _minValue;
        set
        {
            _minValue = value;
            if(IsInstanceValid(Slider)) Slider.MinValue = value;
            if(IsInstanceValid(SpinBox)) SpinBox.MinValue = value;
        }
    }

    private double _maxValue = 100.0;
    [Export] public double MaxValue
    {
        get => _maxValue;
        set
        {
            _maxValue = value;
            if(IsInstanceValid(Slider)) Slider.MaxValue = value;
            if(IsInstanceValid(SpinBox)) SpinBox.MaxValue = value;
        }
    }
    
    private double _value = 0.0;
    [Export] public double Value
    {
        get => _value;
        set
        {
            _value = value;
            if(IsInstanceValid(Slider)) Slider.Value = value;
            if(IsInstanceValid(SpinBox)) SpinBox.Value = value;
        }
    }
    
    private double _step = 0.01;
    [Export] public double Step
    {
        get => _step;
        set
        {
            _step = value;
            if(IsInstanceValid(Slider)) Slider.Step = value;
            if(IsInstanceValid(SpinBox)) SpinBox.Step = value;
        }
    }

    private bool _expEdit = false;
    [Export] public bool ExpEdit
    {
        get => _expEdit;
        set
        {
            _expEdit = value;
            if(IsInstanceValid(Slider)) Slider.ExpEdit = value;
            if(IsInstanceValid(SpinBox)) SpinBox.ExpEdit = value;
        }
    }

    #endregion

    public override void _Ready()
    {
        Slider = new()
        {
            MinValue = _minValue,
            MaxValue = _maxValue,
            Step = _step,
            ExpEdit = _expEdit,
            Value = _value,
            CustomMinimumSize = new(100, 0),
            Scrollable = false,
            SizeFlagsVertical = SizeFlags.ShrinkCenter | SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        SpinBox = new()
        {
            MinValue = _minValue,
            MaxValue = _maxValue,
            Step = _step,
            ExpEdit = _expEdit,
            Value = _value,
        };
        AddChild(Slider);
        AddChild(SpinBox);
        Connect(Slider);
        Connect(SpinBox);
        Slider.SetOwner(this);
        SpinBox.SetOwner(this);
    }

    private void Connect(Godot.Range control)
    {
        control.ValueChanged += value =>
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (Value == value) return;
            Value = value;
            EmitSignal(SignalName.ValueChanged, Value);
        };
    }
}