using System.Collections.Generic;
using Ciallo.Command;
using Ciallo.Data;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Each layer entity gets a full-width <see cref="TrackRowWrapper"/> whose title is a
/// <see cref="TrackRow"/> (HSplitContainer) containing both the
/// <see cref="TrackHeaderBlock"/> (left panel) and — for CelFolder layers — a
/// <see cref="CelTrack"/> (right panel).
/// The split offset of every <see cref="TrackRow"/> is kept in sync with HSplitRuler
/// via <see cref="SplitOffset"/>.
/// </summary>
[SceneTree(root: "Root"), Instantiable]
public partial class TrackTree : LayerTreeBase
{
    private int _splitOffset = 256;

    public override void _Ready()
    {
        InitBase();
    }

    protected override LayerWrapper GetWrapper(Entity e) => e.Get<TrackRowWrapper>();
    protected override ILayerBlock GetBlock(Entity e) => e.Get<TrackHeaderBlock>();
    protected override bool ShouldShowTimelineLayerActions => true;

    /// <summary>Exposes the root wrapper so <see cref="TimelinePanel"/> can register it on the document entity.</summary>
    public TrackRowWrapper RootWrapper => (TrackRowWrapper)RootContainer;

    /// <summary>Shared right-click menu for all <see cref="CelTrack"/> instances. Set by <see cref="TimelinePanel"/>.</summary>
    public CelTrackRightClickMenu RightClickMenu { get; set; }

    /// <summary>
    /// The split offset (in pixels) shared by all <see cref="TrackRow"/> instances in this tree.
    /// Set this whenever HSplitRuler is dragged.
    /// </summary>
    public int SplitOffset
    {
        get => _splitOffset;
        set
        {
            _splitOffset = value;
            UpdateAllSplits(RootContainer, value);
        }
    }

    private static void UpdateAllSplits(Node node, int splitOffset)
    {
        foreach (var child in node.GetChildren())
        {
            // Template rows are HSplitContainers added directly under a wrapper (not a TrackRow title).
            if (child is HSplitContainer templateSplit)
                templateSplit.SplitOffsets = [splitOffset];
            if (child is not TrackRowWrapper wrapper) continue;
            if (wrapper.Title is TrackRow row)
                row.SplitOffsets = [splitOffset];
            UpdateAllSplits(wrapper, splitOffset);
        }
    }

    /// <summary>
    /// Creates a <see cref="TrackRowWrapper"/> + <see cref="TrackRow"/> for
    /// <paramref name="layerE"/> and wires all UI bindings via
    /// <see cref="LayerTreeBase.InitBlock"/>.
    /// For CelFolder layers a <see cref="CelTrack"/> is added to the right panel and
    /// bound to the layer's exposure table and the document's <see cref="TimelineSetting"/>.
    /// Call once per entity from its layer-tree-node <c>Added</c> event handler.
    /// </summary>
    public void Create(Entity layerE)
    {
        var wrapper = new TrackRowWrapper();
        var trackRow = TrackRow.New();
        trackRow.Configure(_splitOffset, wrapper);

        var folderSetting = layerE.TryGet<FolderLayerSetting>();
        if (folderSetting?.IsCelFolder == true)
        {
            var subs = new CompositeDisposable();
            subs.AddTo(layerE);
            trackRow.EnableCelTrack(layerE, RightClickMenu, subs);
            WireCelChildTemplates(layerE, wrapper, folderSetting, subs);
        }

        wrapper.Title = trackRow;
        layerE.Add(trackRow.HeaderBlock);
        layerE.AddNode(wrapper);

        InitBlock(layerE);
    }

    /// <summary>
    /// Renders one template <see cref="LayerBlock"/> per distinct cel-child name under a cel folder,
    /// shown when the folder is expanded (the real cel rows stay hidden via
    /// <see cref="TrackRowWrapper.IsBeingCeled"/>). Reconciles on the debounced add/remove signals
    /// of <see cref="FolderLayerSetting.CelChildrenByName"/>: new key -> create+wire a template,
    /// removed key -> dispose+free its block. Surviving keys are left untouched.
    /// </summary>
    private void WireCelChildTemplates(Entity layerE, TrackRowWrapper wrapper, FolderLayerSetting folderSetting, CompositeDisposable subs)
    {
        var celChildrenByName = folderSetting.CelChildrenByName;
        var blocks = new Dictionary<string, LayerBlock>();
        var blockSubs = new Dictionary<string, CompositeDisposable>();

        void CreateTemplate(string name)
        {
            if (blocks.ContainsKey(name)) return;

            var block = LayerBlock.New();
            block.WorkingButton.Visible = false;
            block.DropdownArrow.Visible = false;
            block.RegularFolderIcon.Visible = false;
            block.CelFolderIcon.Visible = false;
            block.LabelLineEdit.SubmitOnFocusExit(); // once: re-calling would stack FocusExited handlers.

            // Wrap in an HSplitContainer (block left, blank right) so the row obeys the shared
            // SplitOffset like every TrackRow; otherwise the block would stretch full-width across
            // the timeline column. UpdateAllSplits keeps the offset in sync.
            var split = new HSplitContainer { DraggingEnabled = false, SizeFlagsHorizontal = SizeFlags.ExpandFill };
            split.AddThemeStyleboxOverride("split_bar_background", new StyleBoxEmpty());
            split.AddChild(block);
            split.AddChild(new Control());
            split.SplitOffsets = [_splitOffset];
            wrapper.AddChild(split);
            // ponytail: one level deeper than the cel-folder header (Wrapper.Level - 1) so a template
            // reads as an aggregated child row, not a sibling of the cel folder. LayerBlock._EnterTree
            // skips indent for a non-wrapper parent, so this assignment sticks (template rows never re-enter).
            block.Indent.Count = wrapper.Level;
            blocks[name] = block;

            BindTemplate(name, block);
        }

        // (Re)wire a block to a current representative of its name group. Called on create and on every
        // reconcile for surviving keys, so a block always reflects a member that is currently in the group
        // (handles rename-merge: the kept block re-picks a representative from the merged membership).
        void BindTemplate(string name, LayerBlock block)
        {
            if (blockSubs.Remove(name, out var oldSubs)) oldSubs.Dispose();
            if (!celChildrenByName.TryGetValue(name, out var members) || members.Count == 0) return;

            var bs = new CompositeDisposable();
            blockSubs[name] = bs;

            // Display mirrors a representative member; ground truth lives on the layers, the block stores
            // no value of its own. Undo/redo reverts members -> representative fires -> display follows,
            // so the block can never drift from the real state.
            // ponytail: representative is any current member (no "mixed" indicator); members that disagree
            // are not surfaced and keep their own values until the next explicit template edit.
            Entity rep = default;
            foreach (var m in members) { rep = m; break; }
            var repSetting = rep.Get<CommonLayerSetting>();
            repSetting.IsVisible.Subscribe(block.VisibleButton.SetPressedNoSignal).AddTo(bs);
            repSetting.Name.Subscribe(v => { if (block.LabelLineEdit.Text != v) block.LabelLineEdit.Text = v; }).AddTo(bs);

            // Input pushes to every current member as one undoable action.
            block.VisibleButton.OnToggledAsObservable()
                .Subscribe(v => PushToMembers(name, e => e.Get<CommonLayerSetting>().IsVisible, v)).AddTo(bs);
            block.LabelLineEdit.OnTextSubmittedAsObservable()
                .Subscribe(v => PushToMembers(name, e => e.Get<CommonLayerSetting>().Name, v)).AddTo(bs);
        }

        // Overwrite the chosen property on every current member of the named group, in one undoable action.
        void PushToMembers<T>(string name, System.Func<Entity, ReactiveProperty<T>> getProp, T value)
        {
            if (!celChildrenByName.TryGetValue(name, out var members) || members.Count == 0) return;
            var cmd = new CommandBuilder(layerE.Document);
            foreach (var member in members)
                cmd.SetTarget(member).SetProperty(getProp, getProp(member).Value, value);
            cmd.Commit();
        }

        void RemoveTemplate(string name)
        {
            if (blockSubs.Remove(name, out var bs)) bs.Dispose();
            // Free the HSplitContainer wrapper (block's parent), not just the block, or the split is orphaned.
            if (blocks.Remove(name, out var block)) block.GetParent().QueueFree();
        }

        void Reconcile()
        {
            foreach (var name in new List<string>(blocks.Keys))
                if (!celChildrenByName.ContainsKey(name))
                    RemoveTemplate(name);
            foreach (var pair in celChildrenByName)
            {
                if (blocks.TryGetValue(pair.Key, out var block))
                    BindTemplate(pair.Key, block); // surviving key: re-pick representative (handles merge)
                else
                    CreateTemplate(pair.Key);
            }
        }

        celChildrenByName.ObserveDictionaryAdd().Select(_ => Unit.Default)
            .Merge(celChildrenByName.ObserveDictionaryRemove().Select(_ => Unit.Default))
            .DebounceFrame(1, GodotFrameProvider.Process)
            .Subscribe(_ => Reconcile())
            .AddTo(subs);

        Reconcile();
    }
}
