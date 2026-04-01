using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewShapeLayerCmd : CommandBase
{
    public readonly Entity CopyE;

    public NewShapeLayerCmd(Entity copyE = default)
    {
        CopyE = copyE;
    }

    public override IEnumerable<Entity> DoRefEntities => ToEnumerable(TargetE);

    public override void BeforeFirstDo(Entity targetE)
    {
        // Data
        var layerNode = new LayerTreeNode();
        targetE.Add(layerNode);

        var commonSetting = CopyE.IsNull
            ? new CommonLayerSetting
            {
                Name = { Value = $"{"Shape layer".Tr()} {LayerTreeNode.LayerCreationId++}" }
            }
            : CopyE.Get<CommonLayerSetting>().Clone();
        targetE.Add(commonSetting);

        var shapeLayerSetting = CopyE.IsNull
            ? new ShapeLayerSetting()
            : CopyE.Get<ShapeLayerSetting>().Clone();
        targetE.Add(shapeLayerSetting);

        // Others
        ShapeLayerNonDataCreation(targetE);
    }

    public override void Do(Entity targetE)
    {
        targetE.Tag<ToSerializeTag>();
    }

    public override void Undo(Entity targetE)
    {
        targetE.Detach<ToSerializeTag>();
    }

    public static void ShapeLayerNonDataCreation(Entity targetE)
    {
        var layerNode = targetE.Get<LayerTreeNode>();
        // View
        var document = targetE.Document;
        var view = new ShapeLayerView();
        view.ObserveLayerSetting(targetE.Get<CommonLayerSetting>()).AddTo(targetE);
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
            document.Get<LayerContainer>().CreateInsert(targetE, et.Index);

            OnAdd(et.Value, et.Index);
        }).AddTo(targetE);

        layerNode.TreeExited.Subscribe(_ =>
        {
            OnRemove();

            // Layer panel
            document.Get<LayerContainer>().RemoveFree(targetE);
        }).AddTo(targetE);

        layerNode.Moved.Subscribe(et =>
        {
            OnRemove();
            OnAdd(et.Value, et.NewIndex);

            // Layer panel
            document.Get<LayerContainer>().Move([et.OldIndex], [et.NewIndex]);
        }).AddTo(targetE);

        return;

        void OnAdd(Entity parentE, int index)
        {
            // View
            var worldView = document.Get<WorldView>();
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
}