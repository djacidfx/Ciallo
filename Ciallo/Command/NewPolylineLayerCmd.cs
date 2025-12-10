using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewPolylineLayerCmd : CommandBase
{
    private readonly PolylineLayerSetting _setting;
    private CompositeDisposable _subs;

    public NewPolylineLayerCmd(PolylineLayerSetting setting = null)
    {
        _setting = setting?.Clone() ?? new PolylineLayerSetting();
    }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public override void Do(Entity layerE)
    {
        _subs = new();
        _subs.AddTo(layerE);

        // Data
        var root = Document.Get<LayerTreeNode>();
        if (!layerE.Has<LayerTreeNode>())
        {
            layerE.Add(new LayerTreeNode()
            {
                Name = { Value = $"{"Line layer".Tr()} {root.ChildCount + 1}" },
            });
        }

        layerE.Tag<ToSerializeTag>();
        root.AddChild(layerE);
        layerE.Add(_setting);

        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateAdd(layerE);

        // View
        var worldView = Document.Get<WorldView>();
        var layerView = new PolylineLayerView();
        worldView.AddChild(layerView);
        layerE.Add(layerView);
        layerView.SetOwner(worldView);

        var node = layerE.Get<LayerTreeNode>();
        node.RegisterProperties(Document.Get<CommandManager>()).AddTo(_subs);
        node.IsVisible.Subscribe(layerView.SetVisible).AddTo(_subs);
        node.Opacity.Subscribe(v =>
        {
            var color = layerView.SelfModulate;
            color.A = v;
            layerView.SelfModulate = color;
        }).AddTo(_subs);

        // Cursor detection
        var worldArea = Document.Get<WorldCursorDetectionArea>();
        var holder = new PolylineAreaHolder() { ProcessMode = Node.ProcessModeEnum.Disabled };
        worldArea.AddChild(holder);
        layerE.Add(holder);
    }

    public override void Undo(Entity layerE)
    {
        // Cursor detection
        layerE.Get<PolylineAreaHolder>().QueueFree();
        layerE.Remove<PolylineAreaHolder>();

        // View
        layerE.Get<PolylineLayerView>().QueueFree();
        layerE.Remove<PolylineLayerView>();

        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(layerE);

        // Data
        layerE.Remove<PolylineLayerSetting>();
        Document.Get<LayerTreeNode>().RemoveChild(^1);
        layerE.Detach<ToSerializeTag>();

        _subs.Dispose();
    }
}

public partial class PolylineAreaHolder : Node2D
{
    public void SetAreaCursor(Control.CursorShape shape)
    {
        foreach (var child in GetChildren())
        {
            var area = (CursorDetectionArea)child;
            area.MouseDefaultCursorShape = shape;
        }
    }
};