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
    private readonly bool _isCel;

    public NewFolderLayerCmd(Entity copyE = default, bool isCel = false)
    {
        CopyE = copyE;
        _isCel = isCel;
    }

    public override void OnDeletedAsDo() => TargetE.Delete();

    public override void BeforeFirstDo(Entity targetE)
    {
        CreateData(targetE);
        CreateOther(targetE);
    }

    public void CreateData(Entity targetE)
    {
        var layerNode = new LayerTreeNode();
        targetE.Add(layerNode);
        var isCel = CopyE.IsNull ? _isCel : CopyE.Get<FolderLayerSetting>().IsCel;

        var commonSetting = CopyE.IsNull
            ? new CommonLayerSetting { Name = { Value = (isCel ? "Cel folder" : "Folder").Tr() } }
            : CopyE.Get<CommonLayerSetting>().Clone();
        targetE.Add(commonSetting);

        var folderLayerSetting = CopyE.IsNull
            ? new FolderLayerSetting()
            : CopyE.Get<FolderLayerSetting>().Clone();
        folderLayerSetting.IsCel = isCel;
        if (folderLayerSetting.IsCel)
            folderLayerSetting.InitCurrent(Document.Get<SelectionManager>().CurrentFrame, Document.Get<TimelineSetting>().OnionSkinOffsets);
        targetE.Add(folderLayerSetting);
    }

    public void CreateOther(Entity targetE)
    {
        var commonSetting = targetE.Get<CommonLayerSetting>();
        var layerNode = targetE.Get<LayerTreeNode>();
        var folderSetting = targetE.Get<FolderLayerSetting>();

        CompositeDisposable subs = new();
        subs.AddTo(targetE);
        FolderLayerView folderLayerView;
        // View
        if (folderSetting.IsCel)
        {
            var celFolderView = new CelFolderView();
            var timelineSetting = Document.Get<TimelineSetting>();
            var shouldShowOnionSkin = timelineSetting.OnionSkinEnabled
                .CombineLatest(Document.Get<SelectionManager>().WorkingCelFolder,
                    (shouldShow, workingCel) => shouldShow && workingCel == targetE)
                .ToReadOnlyReactiveProperty();

            celFolderView.Observe(folderSetting, layerNode, shouldShowOnionSkin, timelineSetting.OnionSkinMaterials).AddTo(targetE);
            folderLayerView = celFolderView;
        }
        else
        {
            folderLayerView = new FolderLayerView();
        }

        targetE.AddNode(folderLayerView);
        folderLayerView.ObserveLayerSetting(commonSetting).AddTo(targetE);

        // Overlay
        var overlayHolder = new OverlayHolder();
        targetE.AddNode(overlayHolder);

        // Body
        var bodyHolder = new BodyHolder();
        targetE.AddNode(bodyHolder);

        // Layer panel
        targetE.Document.Get<LayerTree>().Create(targetE);
        // Timeline track (creates CelTrack for CelFolders automatically)
        targetE.Document.Get<TrackTree>().Create(targetE);

        // Layer tree events
        var events = layerNode.MovedReparentedAsAddedRemoved;

        events.Added.Subscribe(et =>
        {
            var parentE = et.Parent;
            // Layer panel
            parentE.Get<LayerWrapper>().InsertNodeAt(targetE.Get<LayerWrapper>(), et.Index);

            // Timeline track
            parentE.Get<TrackRowWrapper>().InsertNodeAt(targetE.Get<TrackRowWrapper>(), et.Index);

            // View
            var parentView = parentE.Get<FolderLayerView>();
            parentView.InsertNodeAt(folderLayerView, et.Index);
            folderLayerView.SetOwner(Document.Get<WorldView>());

            // Overlay
            parentE.Get<OverlayHolder>().InsertNodeAt(overlayHolder, et.Index);

            // Body
            parentE.Get<BodyHolder>().InsertNodeAt(bodyHolder, et.Index);
        }).AddTo(targetE);

        events.Removed.Subscribe(_ =>
        {
            // Layer panel
            targetE.Get<LayerWrapper>().RemoveFromParent();

            // Timeline track
            targetE.Get<TrackRowWrapper>().RemoveFromParent();

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


public partial class CommandBuilder
{
    public CommandBuilder NewCelFolder()
    {
        var cmd = new NewFolderLayerCmd(isCel: true) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }
}
