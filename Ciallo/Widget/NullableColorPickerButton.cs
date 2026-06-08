using Godot;
using R3;

namespace Ciallo.Widget;

[Tool, GlobalClass]
public partial class NullableColorPickerButton : ColorPickerButton
{
    public readonly Subject<Color?> ColorOrNullChanged = new();

    public CheckButton HasColorToggle = new()
    {
        Name = "HasColorToggle",
        Text = "Enabled",
    };

    [Export]
    public bool HasColor
    {
        get;
        set
        {
            field = value;
            QueueRedraw();
        }
    } = true;

    public Color? ColorOrNull => HasColor ? Color : null;

    public override void _Ready()
    {
        base._Ready();
        var picker = GetPicker();

        HasColorToggle.SetPressedNoSignal(HasColor);
        HasColorToggle.Toggled += OnHasColorToggled;
        ColorChanged += OnColorChanged;

        // Insert
        var internalMargin = picker.GetChild(0, includeInternal: true);
        var realVbox = internalMargin.GetChild(0);
        var sampleHbc = realVbox.GetChild(1); // sample_hbc
        sampleHbc.AddChild(HasColorToggle);
        sampleHbc.MoveChild(HasColorToggle, 0);
    }

    public NullableColorPickerButton BindColor(ReactiveProperty<Color?> property, CompositeDisposable subs = null)
    {
        if (subs == null)
        {
            subs = new CompositeDisposable();
            subs.AddTo(this);
        }

        property.Subscribe(SetColorOrNullNoSignal).AddTo(subs);

        ColorOrNullChanged
            .Subscribe(color => property.Value = color)
            .AddTo(subs);

        return this;
    }

    public NullableColorPickerButton RegisterUndo(CommandManager manager)
    {
        if (manager == null)
            return this;

        bool innerChange = false;
        var recordedColor = ColorOrNull;
        ColorOrNullChanged
            .Subscribe(newColor =>
            {
                if (innerChange)
                {
                    innerChange = false;
                    return;
                }

                var oldColor = recordedColor;
                manager.CommitSequence(
                    "Change nullable color picker " + Name,
                    new DelegateCommand(
                        () =>
                        {
                            innerChange = true;
                            SetColorOrNullAndEmit(newColor);
                            recordedColor = newColor;
                        },
                        () =>
                        {
                            innerChange = true;
                            SetColorOrNullAndEmit(oldColor);
                            recordedColor = oldColor;
                        }),
                    execute: false);
                recordedColor = newColor;
            })
            .AddTo(this);
        return this;
    }

    public void SetColorOrNullNoSignal(Color? color)
    {
        if (color.HasValue)
        {
            if (!Color.IsEqualApprox(color.Value))
                Color = color.Value;
            HasColor = true;
            HasColorToggle.SetPressedNoSignal(true);
        }
        else
        {
            HasColor = false;
            HasColorToggle.SetPressedNoSignal(false);
        }
    }

    public override void _Draw()
    {
        base._Draw();

        if (HasColor) return;

        var rect = new Rect2(new Vector2(3, 3), Size - new Vector2(6, 6));
        var white = Colors.White;

        DrawRect(rect, new Color(0, 0, 0, 0.18f));
        DrawRect(rect, white, filled: false, width: 1f);
        DrawLine(rect.Position, rect.End, white, width: 2f);
        DrawLine(new Vector2(rect.Position.X, rect.End.Y), new Vector2(rect.End.X, rect.Position.Y), white, width: 2f);
    }

    private void OnHasColorToggled(bool hasColor)
    {
        SetColorOrNullAndEmit(hasColor ? Color : null);
    }

    private void OnColorChanged(Color color)
    {
        SetColorOrNullAndEmit(color);
    }

    private void SetColorOrNullAndEmit(Color? color)
    {
        SetColorOrNullNoSignal(color);
        ColorOrNullChanged.OnNext(ColorOrNull);
    }
}