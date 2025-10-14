using System.Linq;
using Ciallo.Data;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Godot;
using Massive;

namespace Ciallo.Tool;

public class ImageEditHover : HoverBase
{
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
        var rotationButton = manager.AddRectButton(setting.Position, setting.Size);
        rotationButton.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        rotationButton.Rotation = setting.Rotation;
        rotationButton.Scale = Vector2.One * 1.2f;

        // Image move button
        var moveButton = manager.AddRectButton(setting.Position, setting.Size);
        moveButton.MouseDefaultCursorShape = Control.CursorShape.Drag;
        moveButton.Rotation = setting.Rotation;
        
        // Corner buttons
        var corners = layerE.Get<ImageLayerSetting>().GetCorners();
        foreach (var (idx, pos) in corners.Index())
        {
            var b = manager.AddRectButton(pos, 100.0f / 3, WorldButtonFlags.ScreenSize);
            b.MouseDefaultCursorShape = idx % 2 == 0 ? Control.CursorShape.Fdiagsize : Control.CursorShape.Bdiagsize;
        }
    }

    public override void Cancel()
    {
        Document.Get<WorldButtonManager>().Clear();
    }
}