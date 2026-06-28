using Ciallo.Data;
using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// ColorPickerButton-style entry point for choosing a fill brush's marker texture: shows the current
/// marker on a dark backdrop and opens a <see cref="MarkerPickerPopup"/> on press. Two-way bound to the
/// target <see cref="ReactiveProperty{ImageTexture}"/> (the working brush's
/// <see cref="FillBrushSetting.MarkerTexture"/>).
/// </summary>
public partial class MarkerPickerButton : Button
{
    private static readonly Color BackdropColor = new(0.12f, 0.12f, 0.12f);

    private readonly TextureRect _preview;
    private readonly CompositeDisposable _subs = new();
    private MarkerPickerPopup _popup;
    private ReactiveProperty<ImageTexture> _target;

    public MarkerPickerButton()
    {
        AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = BackdropColor });
        AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = BackdropColor.Lightened(0.1f) });
        AddThemeStyleboxOverride("pressed", new StyleBoxFlat { BgColor = BackdropColor });

        _preview = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
        };
        AddChild(_preview);
        _preview.SetAnchorsPreset(LayoutPreset.FullRect);

        Pressed += OpenPopup;
    }

    public void Bind(ReactiveProperty<ImageTexture> markerTexture)
    {
        _target = markerTexture;
        markerTexture.Subscribe(t => _preview.Texture = t).AddTo(_subs);
    }

    private void OpenPopup()
    {
        if (_popup == null)
        {
            _popup = new MarkerPickerPopup();
            AddChild(_popup);
            _popup.Picked.Subscribe(t =>
            {
                if (_target != null)
                    _target.Value = t;
            }).AddTo(_subs);
        }

        _popup.SyncSelection(_target?.Value);

        var rect = GetGlobalRect();
        _popup.Popup(new Rect2I((Vector2I)(rect.Position + new Vector2(0, rect.Size.Y)), Vector2I.Zero));
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
            _subs.Dispose();
    }
}
