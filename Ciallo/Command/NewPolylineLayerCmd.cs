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
    private readonly List<Node> _refObjects = [];
    private readonly PolylineLayerSetting _setting;

    public NewPolylineLayerCmd(PolylineLayerSetting setting = null)
    {
        _setting = setting?.Clone() ?? new PolylineLayerSetting();
    }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(LayerE);
    public override IEnumerable<GodotObject> DoRefObjects => _refObjects;

    public override void Do()
    {
        InitEntity();

        // Data
        var tree = Document.Get<LayerTreeManager>();
        LayerE.Tag<ToSerializeTag>();
        tree.Root.AddChild(LayerE);

        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateAdd(LayerE);

        // View
        var worldView = Document.Get<WorldView>();
        if (_refObjects.Count == 0) _refObjects.Add(new PolylineLayerView());
        var layerView = (PolylineLayerView)_refObjects[0];
        worldView.AddChild(layerView);
        LayerE.Add(layerView);
        layerView.SetOwner(worldView);
        LayerE.Get<LayerTreeNode>().IsVisible.Subscribe(layerView.SetVisible).AddTo(layerView);

        // Cursor detection
        var worldArea = Document.Get<WorldCursorDetectionArea>();
        var holder = new PolylineAreaHolder() { ProcessMode = Node.ProcessModeEnum.Disabled };
        worldArea.AddChild(holder);
        LayerE.Add(holder);
    }

    public override void Undo()
    {
        // Cursor detection
        var holder = LayerE.Get<PolylineAreaHolder>();
        holder.QueueFree();
        LayerE.Remove<PolylineAreaHolder>();

        // View
        LayerE.Remove<PolylineLayerView>();
        var worldView = Document.Get<WorldView>();
        worldView.RemoveChild(_refObjects[0]);

        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(LayerE);

        // Data
        var tree = Document.Get<LayerTreeManager>();
        tree.Root.RemoveChild(^1);
        LayerE.Detach<ToSerializeTag>();
    }

    public Entity InitEntity()
    {
        var tree = Document.Get<LayerTreeManager>();

        if (LayerE.IsNull())
        {
            LayerE = WorkingWorld.Create();
            var node = new LayerTreeNode()
            {
                Name = { Value = $"{"Line layer".Tr()} {tree.Root.ChildCount + 1}" },
            };
            LayerE.Add(_setting);
            LayerE.Add(node);
        }

        return LayerE;
    }
}

public partial class PolylineAreaHolder : Node2D;