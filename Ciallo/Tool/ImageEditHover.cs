using System.Collections.Immutable;
using System.Linq;
using Ciallo.Data;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Godot;
using Massive;

namespace Ciallo.Tool;

public class ImageEditHover : HoverBase
{
    public Button RotationButton;
    public Button MoveButton;
    public ImmutableArray<Button> CornerButtons = [];

    public override bool CanInteract
    {
        get
        {
            var layerE = SelectionManager.WorkingLayer.Value;
            return layerE.IsNotNull() && layerE.Has<ImageLayerSetting>();
        }
    }

    public override void Start(CursorMotionData data)
    {
        var layerE = SelectionManager.WorkingLayer.Value;
        var setting = layerE.Get<ImageLayerSetting>();
        var manager = Document.Get<WorldButtonManager>();

        // Rotation button
        RotationButton = manager.AddRectButton(setting.Position, setting.ImageSize);
        RotationButton.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        RotationButton.Rotation = setting.Rotation;
        RotationButton.Scale = Vector2.One * 1.2f;

        // Image move button
        MoveButton = manager.AddRectButton(setting.Position, setting.ImageSize);
        MoveButton.MouseDefaultCursorShape = Control.CursorShape.Drag;
        MoveButton.Rotation = setting.Rotation;

        // Corner buttons
        var corners = layerE.Get<ImageLayerSetting>().GetCorners();
        var buttons = new Button[corners.Length];
        foreach (var (idx, pos) in corners.Index())
        {
            var b = manager.AddRectButton(pos, 100.0f / 3, WorldButtonFlags.ScreenSize);
            b.MouseDefaultCursorShape = idx % 2 == 0 ? Control.CursorShape.Fdiagsize : Control.CursorShape.Bdiagsize;
            buttons[idx] = b;
        }
        CornerButtons = [..buttons];
    }

    public override void Cancel()
    {
        Document.Get<WorldButtonManager>().Clear();
        RotationButton = null;
        MoveButton = null;
        CornerButtons = [];
    }
}