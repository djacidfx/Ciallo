using Godot;

namespace Ciallo.Widget;

[GlobalClass, Tool]
public partial class Vector2Edit : HBoxContainer
{
    [Signal]
    public delegate void ValueChangedEventHandler(Vector2 newValue);

    public SpinBox SpinX { get; private set; }
    public SpinBox SpinY { get; private set; }

    #region Export

    private double _minValue = 0.0;
    [Export] public double MinValue
    {
        get => _minValue;
        set
        {
            _minValue = value;
            if (IsInstanceValid(SpinX)) SpinX.MinValue = value;
            if (IsInstanceValid(SpinY)) SpinY.MinValue = value;
        }
    }

    private double _maxValue = 100.0;
    [Export] public double MaxValue
    {
        get => _maxValue;
        set
        {
            // Note: Change into null-conditional assignment after upgrading to .net10
            // https://www.arungudelli.com/csharp-tips/null-conditional-assignment-in-csharp/
            _maxValue = value;
            if (IsInstanceValid(SpinX)) SpinX.MaxValue = value;
            if (IsInstanceValid(SpinY)) SpinY.MaxValue = value;
        }
    }

    private double _step = 0;
    [Export] public double Step
    {
        get => _step;
        set
        {
            _step = value;
            if (IsInstanceValid(SpinX)) SpinX.Step = value;
            if (IsInstanceValid(SpinY)) SpinY.Step = value;
        }
    }

    private bool _expEdit = false;
    [Export] public bool ExpEdit
    {
        get => _expEdit;
        set
        {
            _expEdit = value;
            if (IsInstanceValid(SpinX)) SpinX.ExpEdit = value;
            if (IsInstanceValid(SpinY)) SpinY.ExpEdit = value;
        }
    }

    private bool _rounded = false;
    [Export] public bool Rounded
    {
        get => _rounded;
        set
        {
            _rounded = value;
            if (IsInstanceValid(SpinX)) SpinX.Rounded = value;
            if (IsInstanceValid(SpinY)) SpinY.Rounded = value;
        }
    }

    private Vector2 _value = Vector2.Zero;
    [Export] public Vector2 Value
    {
        get => _value;
        set
        {
            _value = value;
            if (IsInstanceValid(SpinX)) SpinX.Value = value.X;
            if (IsInstanceValid(SpinY)) SpinY.Value = value.Y;
        }
    }

    #endregion

    public override void _Ready()
    {
        SpinX = new SpinBox
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.Fill,
            MinValue = MinValue,
            MaxValue = MaxValue,
            Step = Step,
            ExpEdit = ExpEdit,
            Rounded = Rounded,
            Value = Value.X,
        };
        SpinY = new SpinBox
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.Fill,
            MinValue = MinValue,
            MaxValue = MaxValue,
            Step = Step,
            ExpEdit = ExpEdit,
            Rounded = Rounded,
            Value = Value.Y,
        };

        Connect(SpinX, 0);
        Connect(SpinY, 1);

        AddChild(SpinX);
        AddChild(SpinY);
        SpinX.SetOwner(this);
        SpinY.SetOwner(this);
    }

    private void Connect(SpinBox spin, int component)
    {
        spin.ValueChanged += rawValue =>
        {
            float v = (float)rawValue;
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (v == _value[component]) return;
            _value = component == 0
                ? new Vector2(v, _value.Y)
                : new Vector2(_value.X, v);
            ;
            EmitSignal(SignalName.ValueChanged, Value);
        };
    }
}