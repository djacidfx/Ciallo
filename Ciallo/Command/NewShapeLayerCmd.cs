using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Misc;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewShapeLayerCmd : CommandBase
{
    private readonly ShapeLayerSetting _setting;
    private CommonLayerSetting _commonSetting;

    public NewShapeLayerCmd(ShapeLayerSetting setting = null, CommonLayerSetting commonSetting = null)
    {
        _setting = setting?.Clone() ?? new ShapeLayerSetting();
        _commonSetting = commonSetting;
    }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public override void BeforeFirstDo(Entity layerE)
    {
        // Data
        var node = new LayerTreeNode();
        layerE.Add(node);

        _commonSetting ??= new CommonLayerSetting
        {
            Name = { Value = $"{"Shape layer".Tr()} {LayerTreeNode.LayerCreationId++}" }
        };
        layerE.Add(_commonSetting);
        _commonSetting.RegisterProperties(Document.Get<CommandManager>()).AddTo(layerE);
        layerE.Add(_setting);

        // View
        var view = new ShapeLayerView();
        view.ObserveLayerSetting(_commonSetting).AddTo(layerE);
        layerE.AddNode(view);

        // Body
        var bodyHolder = new ShapeBodyHolder() { ProcessMode = Node.ProcessModeEnum.Disabled };
        layerE.AddNode(bodyHolder);
    }

    public override void Do(Entity layerE)
    {
        // Data
        var root = Document.Get<LayerTreeNode>();
        layerE.Tag<ToSerializeTag>();
        root.AddChild(layerE);

        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateAdd(layerE);

        // View
        var worldView = Document.Get<WorldView>();
        var layerView = layerE.Get<ShapeLayerView>();
        worldView.AddChild(layerView);
        layerView.SetOwner(worldView);

        // Body
        var holder = layerE.Get<ShapeBodyHolder>();
        Document.Get<WorldBody>().AddChild(holder);
    }

    public override void Undo(Entity layerE)
    {
        // Body
        layerE.Get<ShapeBodyHolder>().RemoveFromParent();

        // View
        layerE.Get<ShapeLayerView>().RemoveFromParent();

        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(layerE);

        // Data
        Document.Get<LayerTreeNode>().RemoveChild(^1);
        layerE.Detach<ToSerializeTag>();
    }
}

public partial class ShapeBodyHolder : Node2D
{
    public void SetAreaCursor(Control.CursorShape shape)
    {
        foreach (var child in GetChildren())
        {
            var area = (Body)child;
            area.MouseDefaultCursorShape = shape;
        }
    }
};