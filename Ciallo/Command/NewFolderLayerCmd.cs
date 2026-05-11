using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Frent;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewFolderLayerCmd : CommandBase
{
    public readonly Entity CopyE;

    public NewFolderLayerCmd(Entity copyE = default)
    {
        CopyE = copyE;
    }

    public override void OnDeletedAsDo() => TargetE.Delete();

    public override void BeforeFirstDo(Entity targetE)
    {
        // Data
        var layerNode = new LayerTreeNode();
        targetE.Add(layerNode);

        var commonSetting = CopyE.IsNull
            ? new CommonLayerSetting { Name = { Value = "Folder".Tr() } }
            : CopyE.Get<CommonLayerSetting>().Clone();
        targetE.Add(commonSetting);

        var folderLayerSetting = CopyE.IsNull
            ? new FolderLayerSetting()
            : CopyE.Get<FolderLayerSetting>().Clone();
        targetE.Add(folderLayerSetting);

        // View
        var folderLayerView = new FolderLayerView();
        targetE.AddNode(folderLayerView);
        commonSetting.IsVisible.Subscribe(folderLayerView.SetVisible).AddTo(targetE);

        // Overlay
        var overlayHolder = new OverlayHolder();
        targetE.AddNode(overlayHolder);

        // Body
        var bodyHolder = new BodyHolder();
        targetE.AddNode(bodyHolder);

        // Layer panel
        targetE.Document.Get<LayerTree>().Create(targetE);

        // Layer tree events
        var events = layerNode.MovedAsAddedRemoved;

        events.Added.Subscribe(et =>
        {
            var parentE = et.Parent;
            // Layer panel
            parentE.Get<LayerWrapper>().InsertNodeAt(targetE.Get<LayerWrapper>(), et.Index);

            // View
            var parentView = parentE.Get<FolderLayerView>();
            parentView.InsertNodeAt(folderLayerView, et.Index);
            folderLayerView.SetOwner(parentView.Owner ?? parentView);

            // Overlay
            parentE.Get<OverlayHolder>().InsertNodeAt(overlayHolder, et.Index);

            // Body
            parentE.Get<BodyHolder>().InsertNodeAt(bodyHolder, et.Index);
        }).AddTo(targetE);

        events.Removed.Subscribe(_ =>
        {
            // Layer panel
            targetE.Get<LayerWrapper>().RemoveFromParent();

            // Body
            bodyHolder.RemoveFromParent();

            // Overlay
            overlayHolder.RemoveFromParent();

            // View
            folderLayerView.RemoveFromParent();
        }).AddTo(targetE);
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