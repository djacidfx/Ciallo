using System.Collections.Generic;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.GuiControl;

[SceneTree, Instantiable]
public partial class StrokeBrushPreviewList : Container
{
    protected readonly Dictionary<Entity, Control> PreviewMap = [];
    protected ISynchronizedView<Entity, Control> SyncView;
    protected ObservableList<Entity> Brushes;
    protected ReactiveProperty<Entity> WorkingBrush;
    protected Entity Document;
    private CompositeDisposable _subs;

    public void Init(Entity document)
    {
        Document = document;
        var sm = Document.Get<SelectionManager>();
        var bm = Document.Get<BrushManager>();
        Bind(bm.StrokeBrushEs, sm.WorkingStrokeBrush);
    }

    public void Bind(ObservableList<Entity> brushes, ReactiveProperty<Entity> workingBrush)
    {
        Brushes = brushes;
        _subs?.Dispose();
        _subs = new();
        SyncView = brushes.CreateView(GetOrCreateBrushPreview);
        SyncView.AddTo(_subs);

        PreviewList.BindChildren(SyncView.ToNotifyCollectionChanged());

        WorkingBrush = workingBrush;
        workingBrush.Subscribe(e =>
        {
            PreviewList.SelectedControl = e.IsNull ? null : GetOrCreateBrushPreview(e);
        }).AddTo(_subs);

        PreviewList.Moved += (src, dst) =>
        {
            Brushes.Move(src, dst);
        };
    }

    private Control GetOrCreateBrushPreview(Entity e)
    {
        if (PreviewMap.TryGetValue(e, out var box))
        {
            return box;
        }
        box = CreateBrushPreview(e);
        PreviewMap.Add(e, box);
        e.OnDelete += ent => PreviewMap.Remove(ent);
        return box;
    }

    private Control CreateBrushPreview(Entity e)
    {
        var textureRect = new TextureRect()
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspect,
            Texture = e.Get<ViewportTexture>(),
        };
        textureRect.QueueFreeWith(e);
        return textureRect;
    }

    public override void _Ready()
    {
        PreviewList.ItemClicked += idx =>
        {
            WorkingBrush.Value = SyncView.Filtered.ElementAt(idx).Value;
        };

        AddButton.Pressed += () => OnAddOrCopyButtonPressed();

        CopyButton.Pressed += () => OnAddOrCopyButtonPressed(WorkingBrush.Value);

        RemoveButton.Pressed += () =>
        {
            var oldE = WorkingBrush.Value;
            if (oldE.IsNull) return;
            var es = SyncView.Filtered.Select(tup => tup.Value).ToList();
            var oldIdx = es.IndexOf(oldE);
            if (oldIdx == -1) return;
            int nextIdx = oldIdx == es.Count - 1 ? oldIdx - 1 : oldIdx + 1;
            Entity nextWorking = nextIdx == -1 ? Entity.Null : es[nextIdx];

            new CommandBuilder(Document)
                .SetProperty(e => e.Get<SelectionManager>().WorkingStrokeBrush, nextWorking)
                .SetTarget(oldE)
                .DeleteBrush()
                .Commit();
        };

        EditButton.Pressed += () => Document.Get<StrokeBrushEditor>().Popup();
    }

    private void OnAddOrCopyButtonPressed(Entity copyE = default)
    {
        var brushE = Document.World.Create();
        new CommandBuilder(brushE)
            .NewStrokeBrush(copyE)
            .SetTarget(Document)
            .SetProperty(e => e.Get<SelectionManager>().WorkingStrokeBrush, brushE)
            .Commit();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            SyncView?.Dispose();
        }
    }
}