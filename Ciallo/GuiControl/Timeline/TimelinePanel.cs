using Ciallo.Data;
using Frent;
using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Timeline panel — owns the shared zoom / scroll state and wires all sub-controls.
/// </summary>
[SceneTree, Instantiable]
public partial class TimelinePanel : VBoxContainer
{
    private ZoomableHScrollBar _zoomScrollBar;
    private TimelineRuler _ruler;
    private BackgroundGrid _bgGrid;
    private Playhead _playhead;
    private PlaybackBar _startBar;
    private PlaybackBar _endBar;
    private HSplitContainer _hSplitRuler;
    private TrackTree _trackTree;
    private HSplitContainer _hSplitScroll;
    private HSplitContainer _hSplitBgGrid;
    private CelTrackRightClickMenu _celTrackRightClickMenu;

    public override void _Ready()
    {
        _zoomScrollBar = GetNode<ZoomableHScrollBar>("%ZoomableHScrollBar");
        _ruler = GetNode<TimelineRuler>("%TimelineRuler");
        _bgGrid = GetNode<BackgroundGrid>("%BackgroundGrid");
        _playhead = GetNode<Playhead>("%Playhead");
        _startBar = GetNode<PlaybackBar>("%PlaybackStartBar");
        _endBar = GetNode<PlaybackBar>("%PlaybackEndBar");
        _hSplitRuler = GetNode<HSplitContainer>("%HSplitRuler");
        _trackTree = GetNode<TrackTree>("%TrackTree");
        _hSplitScroll = GetNode<HSplitContainer>("%HSplitScrollBar");
        _hSplitBgGrid = GetNode<HSplitContainer>("%HSplitBgGrid");
        _celTrackRightClickMenu = GetNode<CelTrackRightClickMenu>("%CelTrackRightClickMenu");
        _trackTree.RightClickMenu = _celTrackRightClickMenu;

        // Keep all track-row splits, the scrollbar spacer, and the BackgroundGrid in lockstep.
        _hSplitRuler.Dragged += offset =>
        {
            _trackTree.SplitOffset = (int)offset;
            _hSplitScroll.SplitOffsets = [(int)offset];
            _hSplitBgGrid.SplitOffsets = [(int)offset];
        };
    }

    /// <summary>
    /// Wire the document's <see cref="TimelineSetting"/> into all sub-controls.
    /// Must be called once after this panel is added to the tree, before <see cref="InitTrackTree"/>.
    /// </summary>
    public TimelinePanel BindTimeline(TimelineSetting setting, ReactiveProperty<int> currentFrame)
    {
        _zoomScrollBar.Setup(setting);

        _ruler.Observe(setting.PixelsPerFrame, setting.ScrollOffsetFrame, setting.FrameRate);
        _ruler.BindPlaybackRange(setting.PlaybackStart, setting.PlaybackEnd);
        _ruler.BindCurrentFrame(currentFrame);

        _bgGrid.Observe(setting.PixelsPerFrame, setting.ScrollOffsetFrame);

        // Start bar: green line + left-handle at PlaybackStart frame
        _startBar.IsStart = true;
        _startBar.Observe(setting.PixelsPerFrame, setting.ScrollOffsetFrame, setting.PlaybackStart, _bgGrid, _ruler);

        // End bar: red line + right-handle at PlaybackEnd frame
        _endBar.IsStart = false;
        _endBar.LineColor = new Color(0.9f, 0.25f, 0.25f, 0.9f);
        _endBar.Observe(setting.PixelsPerFrame, setting.ScrollOffsetFrame, setting.PlaybackEnd, _bgGrid, _ruler);

        _playhead.Observe(setting.PixelsPerFrame, setting.ScrollOffsetFrame, currentFrame, _bgGrid);

        TimelineAction.FrameRate.BindNumber(setting.FrameRate);

        return this;
    }

    /// <summary>
    /// Registers <see cref="TrackTree"/>, its root wrapper, and <see cref="BackgroundGrid"/>
    /// as Frent components on <paramref name="document"/> so that layer commands can
    /// create and position track rows at runtime.
    /// Must be called once after <see cref="BindTimeline"/> and after the panel is added to the tree.
    /// </summary>
    public void InitTrackTree(Entity document)
    {
        document.Add(_trackTree);
        document.Add(_trackTree.RootWrapper);
        document.Add(_bgGrid);
        _ruler.BindSelectionManager(document.Get<SelectionManager>());
        _celTrackRightClickMenu.InitDocument(document);
    }
}