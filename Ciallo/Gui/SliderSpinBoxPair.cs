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
    
    public double MaxValue
    {
        set
        {
            Slider.MaxValue = value;
            SpinBox.MaxValue = value;
        }
    }
    
    public double MinValue
    {
        set
        {
            Slider.MinValue = value;
            SpinBox.MinValue = value;
        }
    }
    
    public double Value
    {
        set
        {
            Slider.Value = value;
            SpinBox.Value = value;
        }
    }
    
    public double Step
    {
        set
        {
            Slider.Step = value;
            SpinBox.Step = value;
        }
    }

    public bool ExpEdit
    {
        set
        {
            Slider.ExpEdit = value;
            SpinBox.ExpEdit = value;
        }
    }
}