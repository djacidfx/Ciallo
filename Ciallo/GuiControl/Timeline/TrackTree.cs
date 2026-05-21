using Ciallo.Data;
using Frent;
using Godot;
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
/// <remarks> Get AI slop into this class and related places since shen's laziness. Refactor on implementing new needs. </remarks>
[SceneTree(root: "Root"), Instantiable]
public partial class TrackTree : LayerTreeBase
{
    private int _splitOffset = 256;

    public override void _Ready()
    {
        InitBase();
    }

    protected override LayerWrapper GetWrapper(Entity e) => e.Get<TrackRowWrapper>();
    protected override LayerBlock GetBlock(Entity e) => e.Get<TrackHeaderBlock>();

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
        var trackRow = new TrackRow();
        var headerBlock = TrackHeaderBlock.New();

        trackRow.DraggingEnabled = false;
        trackRow.SplitOffsets = [_splitOffset];
        trackRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var scrollContainer = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.Fill,
            ClipContents = false,
            HorizontalScrollMode = ScrollMode.ShowNever,
            VerticalScrollMode = ScrollMode.Disabled,
        };

        scrollContainer.AddChild(headerBlock);
        trackRow.AddChild(scrollContainer);
        trackRow.HeaderBlock = headerBlock;
        headerBlock.OwningWrapper = wrapper;

        var folderSetting = layerE.TryGet<FolderLayerSetting>();
        if (folderSetting?.IsCel == true)
        {
            var subs = new CompositeDisposable();
            subs.AddTo(layerE);

            var celTrack = new CelTrack();
            var timeSetting = layerE.Document.Get<TimelineSetting>();
            celTrack.Observe(timeSetting.PixelsPerFrame, timeSetting.ScrollOffsetFrame, timeSetting.PlaybackStart, timeSetting.PlaybackEnd, subs);
            var selectionManager = layerE.Document.Get<SelectionManager>();
            celTrack.Bind(layerE, folderSetting.Exposures, selectionManager, subs);
            celTrack.RightClickMenu = RightClickMenu;
            trackRow.AddChild(celTrack);
            trackRow.CelTrack = celTrack;
            layerE.Add(celTrack);
        }
        else
        {
            // Empty placeholder keeps the HSplitContainer's right panel present.
            var placeholder = new Control
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            trackRow.AddChild(placeholder);
        }

        wrapper.Title = trackRow;
        layerE.Add((TrackHeaderBlock)headerBlock);
        layerE.AddNode(wrapper);

        InitBlock(layerE);
    }
}