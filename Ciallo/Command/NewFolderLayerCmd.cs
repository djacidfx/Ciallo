using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Frent;
using ObservableCollections;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewFolderLayerCmd : CommandBase
{
    public readonly Entity CopyE;
    private bool _isCelFolder;

    public NewFolderLayerCmd(Entity copyE = default, bool isCelFolder = false)
    {
        CopyE = copyE;
        _isCelFolder = isCelFolder;
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
        _isCelFolder = CopyE.IsNull ? _isCelFolder : CopyE.Get<FolderLayerSetting>().IsCelFolder;

        var commonSetting = CopyE.IsNull
            ? new CommonLayerSetting { Name = { Value = (_isCelFolder ? "Cel folder" : "Folder").Tr() } }
            : CopyE.Get<CommonLayerSetting>().Clone();
        targetE.Add(commonSetting);

        var folderLayerSetting = CopyE.IsNull
            ? new FolderLayerSetting()
            : CopyE.Get<FolderLayerSetting>().Clone();
        folderLayerSetting.IsCelFolder = _isCelFolder;
        if (folderLayerSetting.IsCelFolder)
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
        if (folderSetting.IsCelFolder)
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

        // Layer tree self events
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

        // Cel folder specific handling for name lookup
        if (_isCelFolder)
        {
            var childNameLookupSubs = new Dictionary<Entity, CompositeDisposable>();
            var celChildrenByName = folderSetting.CelChildrenByName;

            void AddCelChildNameLookupEntry(string name, Entity layerE)
            {
                if (!celChildrenByName.TryGetValue(name, out var layers))
                {
                    layers = [];
                    celChildrenByName[name] = layers;
                }

                layers.Add(layerE);
            }

            void RemoveCelChildNameLookupEntry(string name, Entity layerE)
            {
                var layers = celChildrenByName[name];
                layers.Remove(layerE);
                if (layers.Count == 0)
                    celChildrenByName.Remove(name);
            }

            layerNode.ObserveAddChild().Subscribe(et =>
            {
                Entity newChildE = et.Value;
                newChildE.Tag<CelTag>();
                if (!newChildE.Has<FolderLayerSetting>()) return;

                ChildLayerNameLookup nameLookup = new(newChildE);
                newChildE.Add(nameLookup);
                nameLookup.Subscribe();

                foreach (var (layerE, name) in nameLookup.Names)
                    AddCelChildNameLookupEntry(name, layerE);

                var subs = new CompositeDisposable();
                childNameLookupSubs[newChildE] = subs;

                nameLookup.Names.ObserveAdd().Subscribe(addEvent =>
                {
                    string newName = addEvent.Value.Value;
                    Entity layerE = addEvent.Value.Key;
                    AddCelChildNameLookupEntry(newName, layerE);
                }).AddTo(subs);

                nameLookup.Names.ObserveReplace().Subscribe(replaceEvent =>
                {
                    string oldName = replaceEvent.OldValue.Value;
                    string newName = replaceEvent.NewValue.Value;
                    Entity layerE = replaceEvent.NewValue.Key;

                    RemoveCelChildNameLookupEntry(oldName, layerE);
                    AddCelChildNameLookupEntry(newName, layerE);
                }).AddTo(subs);

                nameLookup.Names.ObserveRemove().Subscribe(removeEvent =>
                {
                    string removedName = removeEvent.Value.Value;
                    Entity layerE = removeEvent.Value.Key;

                    RemoveCelChildNameLookupEntry(removedName, layerE);
                }).AddTo(subs);
            }).AddTo(targetE);

            layerNode.ObserveRemoveChild().Subscribe(et =>
            {
                Entity childE = et.Value;
                childE.Detach<CelTag>();
                if (!childE.Has<ChildLayerNameLookup>()) return;

                var nameLookup = childE.GetRemove<ChildLayerNameLookup>();
                nameLookup.Unsubscribe();

                foreach (var (layerE, name) in nameLookup.Names)
                    RemoveCelChildNameLookupEntry(name, layerE);

                childNameLookupSubs.Remove(childE, out var subs);
                subs.Dispose();
            }).AddTo(targetE);
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

public partial class CommandBuilder
{
    public CommandBuilder NewCelFolder()
    {
        var cmd = new NewFolderLayerCmd(isCelFolder: true) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }
}
