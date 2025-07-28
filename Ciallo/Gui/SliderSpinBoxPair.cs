using Ciallo.Misc;
using Godot;
using R3;

namespace Ciallo.Gui;

public partial class SliderSpinBoxPair : HBoxContainer
{
    public readonly HSlider Slider = new();
    public readonly SpinBox SpinBox = new();
    
    public SliderSpinBoxPair()
    {
        AddChild(Slider);
        Slider.CustomMinimumSize = new Vector2(100, 0);
        Slider.Scrollable = false;
        Slider.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        AddChild(SpinBox);
    }
    
    public void BindValue<T>(ReactiveProperty<T> property) where T : System.Numerics.INumber<T>
    {
        Slider.BindValue(property);
        SpinBox.BindValue(property);
    }
    
    [Export] public double MaxValue
    {
        get => Slider.MaxValue;
        set
        {
            Slider.MaxValue = value;
            SpinBox.MaxValue = value;
        }
    }
    
    [Export] public double MinValue
    {
        get => Slider.MinValue;
        set
        {
            Slider.MinValue = value;
            SpinBox.MinValue = value;
        }
    }
    
    [Export] public double Value
    {
        get => Slider.Value;
        set
        {
            Slider.Value = value;
            SpinBox.Value = value;
        }
    }
    
    [Export] public double Step
    {
        get => Slider.Step;
        set
        {
            Slider.Step = value;
            SpinBox.Step = value;
        }
    }

    [Export] public bool ExpEdit
    {
        get => Slider.ExpEdit;
        set
        {
            Slider.ExpEdit = value;
            SpinBox.ExpEdit = value;
        }
    }
}