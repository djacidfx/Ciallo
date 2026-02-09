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
public class NewPolylineLayerCmd : CommandBase
{
    private readonly PolylineLayerSetting _setting;
    private CommonLayerSetting _commonSetting;
    private CompositeDisposable _subs;

    public NewPolylineLayerCmd(PolylineLayerSetting setting = null, CommonLayerSetting commonSetting = null)
    {
        _setting = setting?.Clone() ?? new PolylineLayerSetting();
        _commonSetting = commonSetting;
    }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public override void BeforeFirstDo(Entity layerE)
    {
        layerE.Add(new LayerTreeNode());

        _commonSetting ??= new CommonLayerSetting
        {
            Name = { Value = $"{"Line layer".Tr()} {LayerTreeNode.LayerCreationId++}" }
        };
        layerE.Add(_commonSetting);
        _commonSetting.RegisterProperties(Document.Get<CommandManager>()).AddTo(layerE);
        layerE.Add(_setting);

        // View
        var layerView = new PolylineLayerView();
        layerE.AddNode(layerView);

        // Body
        var holder = new PolylineBodyHolder() { ProcessMode = Node.ProcessModeEnum.Disabled };
        layerE.AddNode(holder);
    }

    public override void Do(Entity layerE)
    {
        _subs = new();
        _subs.AddTo(layerE);

        // Data
        var root = Document.Get<LayerTreeNode>();
        layerE.Tag<ToSerializeTag>();
        root.AddChild(layerE);

        // Layer panel
        var layerContainer = Document.Get<LayerContainer>();
        layerContainer.CreateAdd(layerE);

        // View
        var worldView = Document.Get<WorldView>();
        var layerView = layerE.Get<PolylineLayerView>();
        worldView.AddChild(layerView);
        layerView.SetOwner(worldView);
        layerView.ObserveLayerSetting(_commonSetting).AddTo(_subs);

        // Body
        var holder = layerE.Get<PolylineBodyHolder>();
        Document.Get<WorldBody>().AddChild(holder);
    }

    public override void Undo(Entity layerE)
    {
        // Body
        layerE.Get<PolylineBodyHolder>().RemoveFromParent();

        // View
        layerE.Get<PolylineLayerView>().RemoveFromParent();

        // Layer panel
        var layerTreeControl = Document.Get<LayerContainer>();
        layerTreeControl.RemoveFree(layerE);

        // Data
        Document.Get<LayerTreeNode>().RemoveChild(^1);
        layerE.Detach<ToSerializeTag>();

        _subs.Dispose();
    }
}

public partial class PolylineBodyHolder : Node2D
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