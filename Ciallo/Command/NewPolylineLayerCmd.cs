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

    protected override void BeforeFirstDo(Entity layerE)
    {
        layerE.Add(new LayerTreeNode());

        _commonSetting ??= new CommonLayerSetting
        {
            Name = { Value = $"{"Line layer".Tr()} {LayerTreeNode.LayerCreationId++}" }
        };
        layerE.Add(_commonSetting);
        _commonSetting.RegisterProperties(Document.Get<CommandManager>()).AddTo(layerE);
        layerE.Add(_setting);
    }

    protected override void Do(Entity layerE)
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
        var layerView = new PolylineLayerView();
        worldView.AddChild(layerView);
        layerE.Add(layerView);
        layerView.SetOwner(worldView);
        layerView.ObserveLayerSetting(_commonSetting).AddTo(_subs);

        // Body
        var holder = new PolylineBodyHolder() { ProcessMode = Node.ProcessModeEnum.Disabled };
        Document.Get<WorldBody>().AddChild(holder);
        layerE.Add(holder);
    }

    protected override void Undo(Entity layerE)
    {
        // Body
        layerE.Get<PolylineBodyHolder>().QueueFree();
        layerE.Remove<PolylineBodyHolder>();

        // View
        layerE.Get<PolylineLayerView>().QueueFree();
        layerE.Remove<PolylineLayerView>();

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