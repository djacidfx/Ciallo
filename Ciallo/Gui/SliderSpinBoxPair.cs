using Ciallo.Misc;
using Godot;
using R3;

namespace Ciallo.Gui;

public partial class SliderSpinBoxPair : HBoxContainer
{
    public readonly HSlider Slider;
    public readonly SpinBox SpinBox;
    
    public SliderSpinBoxPair()
    {
        Slider = new()
        {
            CustomMinimumSize = new Vector2(100, 0),
            Scrollable = false,
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        SpinBox = new();
        AddChild(Slider);
        AddChild(SpinBox);
        Slider.SetOwner(this);
        SpinBox.SetOwner(this);
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