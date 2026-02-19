using Godot;

namespace Ciallo.Widget;

/// <summary>
/// This spin slider aims to be same as Godot's EditorSpinSlider. Current version is visually different.
/// </summary>
[GlobalClass, Tool]
public partial class SpinSlider : HBoxContainer
{
    [Signal]
    public delegate void ValueChangedEventHandler(double oldValue, double newValue);

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
            if (IsInstanceValid(Slider)) Slider.MinValue = value;
        }
    }

    private double _maxValue = 100.0;
    [Export] public double MaxValue
    {
        get => _maxValue;
        set
        {
            _maxValue = value;
            if (IsInstanceValid(Slider)) Slider.MaxValue = value;
        }
    }

    private double _value = 0.0;
    [Export] public double Value
    {
        get => _value;
        set
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_value == value) return;
            var oldValue = _value;
            _value = value;
            if (IsInstanceValid(Slider)) Slider.SetValueNoSignal(value);
            EmitSignalValueChanged(oldValue, value);
        }
    }

    private double _step = 0.01;
    [Export] public double Step
    {
        get => _step;
        set
        {
            _step = value;
            if (IsInstanceValid(Slider)) Slider.Step = value;
        }
    }

    private bool _expEdit = false;
    [Export] public bool ExpEdit
    {
        get => _expEdit;
        set
        {
            _expEdit = value;
            if (IsInstanceValid(Slider)) Slider.ExpEdit = value;
        }
    }

    private bool _allowLesser = false;
    [Export] public bool AllowLesser
    {
        get => _allowLesser;
        set
        {
            _allowLesser = value;
            if (IsInstanceValid(Slider)) Slider.AllowLesser = value;
        }
    }

    private bool _allowGreater = false;
    [Export] public bool AllowGreater
    {
        get => _allowGreater;
        set
        {
            _allowGreater = value;
            if (IsInstanceValid(Slider)) Slider.AllowGreater = value;
        }
    }

    private bool _rounded = false;
    [Export] public bool Rounded
    {
        get => _rounded;
        set
        {
            _rounded = value;
            if (IsInstanceValid(Slider)) Slider.Rounded = value;
            if (IsInstanceValid(SpinBox)) SpinBox.Rounded = value;
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
            AllowLesser = _allowLesser,
            AllowGreater = _allowGreater,
            Rounded = _rounded,
            Value = _value,
            CustomMinimumSize = new(64, 0),
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
            AllowLesser = _allowLesser,
            AllowGreater = _allowGreater,
            Rounded = _rounded,
            Value = _value,
        };

        Slider.Share(SpinBox);
        AddChild(Slider);
        AddChild(SpinBox);

        Slider.ValueChanged += v => Value = v;

        // Pitfall: LineEdit has no way to show rounded number without modifying the number itself.
        // Have to do this manually.
        // // This not work
        // SpinBox.GetLineEdit().TextChanged += text =>
        // {
        //     if (text.Length > 6)
        //         SpinBox.GetLineEdit().Text = text[..6];
        // };
        SpinBox.GetLineEdit().TextDirection = TextDirection.Ltr;
        SpinBox.GetLineEdit().SubmitOnFocusExit();
    }

    public void SetValueNoSignal(double value)
    {
        _value = value;
        if (IsInstanceValid(Slider))
            Slider.SetValueNoSignal(value);
    }

    public SpinSlider RegisterUndo(CommandManager manager)
    {
        // block inner change to avoid infinite loop.
        bool innerChange = false;
        ValueChanged += (oldValue, newValue) =>
        {
            if (innerChange)
            {
                innerChange = false;
                return;
            }
            manager.CreateAction("Change value of SpinSlider " + GetInstanceId(), UndoRedo.MergeMode.Ends);
            Engine.PrintErrorMessages = false;
            manager.AddDoMethod(Callable.From(() =>
            {
                innerChange = true;
                Value = newValue;
            }));
            manager.AddUndoMethod(Callable.From(() =>
            {
                innerChange = true;
                Value = oldValue;
            }));
            Engine.PrintErrorMessages = true;
            manager.CommitAction(false);
        };
        return this;
    }
}