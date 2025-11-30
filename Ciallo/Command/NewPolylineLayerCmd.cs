using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

public class NewPolylineLayerCmd : CommandBase
{
    public Entity LayerE;
    private readonly PolylineLayerSetting _setting;
    private CompositeDisposable _subs;

    public NewPolylineLayerCmd(PolylineLayerSetting setting = null)
    {
        _setting = setting?.Clone() ?? new PolylineLayerSetting();
        InitEntity();
    }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(LayerE);

    public override void Do()
    {
        _subs = new();
        _subs.AddTo(LayerE);

        // Data
        var root = Document.Get<LayerTreeNode>();
        LayerE.Tag<ToSerializeTag>();
        root.AddChild(LayerE);
        LayerE.Add(_setting);

        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateAdd(LayerE);

        // View
        var worldView = Document.Get<WorldView>();
        var layerView = new PolylineLayerView();
        worldView.AddChild(layerView);
        LayerE.Add(layerView);
        layerView.SetOwner(worldView);

        var node = LayerE.Get<LayerTreeNode>();
        node.RegisterToCommandManager(Document.Get<CommandManager>()).AddTo(_subs);
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
        LayerE.Add(holder);
    }

    public override void Undo()
    {
        // Cursor detection
        LayerE.Get<PolylineAreaHolder>().QueueFree();
        LayerE.Remove<PolylineAreaHolder>();

        // View
        LayerE.Get<PolylineLayerView>().QueueFree();
        LayerE.Remove<PolylineLayerView>();

        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(LayerE);

        // Data
        LayerE.Remove<PolylineLayerSetting>();
        Document.Get<LayerTreeNode>().RemoveChild(^1);
        LayerE.Detach<ToSerializeTag>();

        _subs.Dispose();
    }

    public Entity InitEntity()
    {
        if (!LayerE.IsNull) return LayerE;

        var root = Document.Get<LayerTreeNode>();
        LayerE = WorkingWorld.Create();
        var node = new LayerTreeNode()
        {
            Name = { Value = $"{"Line layer".Tr()} {root.ChildCount + 1}" },
        };
        LayerE.Add(node);

        return LayerE;
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