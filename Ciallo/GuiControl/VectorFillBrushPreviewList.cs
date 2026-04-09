using System.Collections.Generic;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.GuiControl;

[SceneTree, Instantiable]
public partial class VectorFillBrushPreviewList : Container
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
        Bind(bm.VectorFillBrushEs, sm.WorkingVectorFillBrush);
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

    private PanelContainer CreateBrushPreview(Entity e)
    {
        var box = new PanelContainer().QueueFreeWith(e);
        var background = new ColorRect() { Material = AutoloadRendering.CheckerboardMaterial };
        box.AddChild(background);
        var markerPreview = new TextureRect()
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            CustomMinimumSize = new(32, 32),
        };
        var container = new CenterContainer();
        container.AddChild(markerPreview);
        box.AddChild(container);

        var setting = e.Get<VectorFillBrushSetting>();
        setting.MarkerTexture.Subscribe(markerPreview.SetTexture).AddTo(e);
        setting.MarkerColor.Subscribe(markerPreview.SetModulate).AddTo(e);
        setting.FillColor.Subscribe(background.SetColor).AddTo(e);
        return box;
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
                .SetProperty(e => e.Get<SelectionManager>().WorkingVectorFillBrush, nextWorking)
                .SetTarget(oldE)
                .DeleteBrush()
                .Commit();
        };
    }

    private void OnAddOrCopyButtonPressed(Entity copyE = default)
    {
        var brushE = Document.World.Create();
        new CommandBuilder(brushE)
            .NewVectorFillBrush(copyE)
            .SetTarget(Document)
            .SetProperty(e => e.Get<SelectionManager>().WorkingVectorFillBrush, brushE)
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