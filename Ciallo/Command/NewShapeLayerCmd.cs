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

    public override void BeforeFirstDo(Entity targetE)
    {
        // Data
        var layerNode = new LayerTreeNode();
        targetE.Add(layerNode);

        _commonSetting ??= new CommonLayerSetting
        {
            Name = { Value = $"{"Shape layer".Tr()} {LayerTreeNode.LayerCreationId++}" }
        };
        targetE.Add(_commonSetting);
        targetE.Add(_setting);

        // View
        var view = new ShapeLayerView();
        view.ObserveLayerSetting(_commonSetting).AddTo(targetE);
        targetE.AddNode(view);

        // Overlay
        var overlayHolder = new OverlayHolder();
        targetE.AddNode(overlayHolder);

        // Body
        var bodyHolder = new BodyHolder() { ProcessMode = Node.ProcessModeEnum.Disabled };
        targetE.AddNode(bodyHolder);

        // Layer tree events
        layerNode.TreeEntered.Subscribe(et =>
        {
            // Layer panel
            Document.Get<LayerContainer>().CreateInsert(targetE, et.Index);

            OnAdd(et.Value, et.Index);
        }).AddTo(targetE);

        layerNode.TreeExited.Subscribe(_ =>
        {
            OnRemove();

            // Layer panel
            Document.Get<LayerContainer>().RemoveFree(targetE);
        }).AddTo(targetE);

        layerNode.Moved.Subscribe(et =>
        {
            OnRemove();
            OnAdd(et.Value, et.NewIndex);

            // Layer panel
            Document.Get<LayerContainer>().Move([et.OldIndex], [et.NewIndex]);
        }).AddTo(targetE);

        return;

        void OnAdd(Entity parentE, int index)
        {
            // View
            var worldView = Document.Get<WorldView>();
            worldView.InsertNodeAt(view, index);
            view.SetOwner(worldView);

            // Overlay
            parentE.Get<OverlayHolder>().InsertNodeAt(overlayHolder, index);

            // Body
            parentE.Get<BodyHolder>().InsertNodeAt(bodyHolder, index);
        }

        void OnRemove()
        {
            // Body
            bodyHolder.RemoveFromParent();

            // Overlay
            overlayHolder.RemoveFromParent();

            // View
            view.RemoveFromParent();
        }
    }

    public override void Do(Entity targetE)
    {
        targetE.Tag<ToSerializeTag>();
    }

    public override void Undo(Entity targetE)
    {
        targetE.Detach<ToSerializeTag>();
    }
}