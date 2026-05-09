using Ciallo.Data;
using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Timeline panel — owns the shared zoom / scroll state and wires all sub-controls.
/// Call <see cref="BindTimeline"/> first, then <see cref="BindPlayhead"/>.
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
    private SpinBox _frameRateSpinBox;
    private HSplitContainer _hSplitRuler;
    private HSplitContainer _hSplitTrack;

    public override void _Ready()
    {
        _zoomScrollBar = GetNode<ZoomableHScrollBar>("%ZoomableHScrollBar");
        _ruler = GetNode<TimelineRuler>("%TimelineRuler");
        _bgGrid = GetNode<BackgroundGrid>("%BackgroundGrid");
        _playhead = GetNode<Playhead>("%Playhead");
        _startBar = GetNode<PlaybackBar>("%PlaybackStartBar");
        _endBar = GetNode<PlaybackBar>("%PlaybackEndBar");
        _frameRateSpinBox = GetNode<SpinBox>("%FrameRateSpinBox");
        _hSplitRuler = GetNode<HSplitContainer>("%HSplitContainer");
        _hSplitTrack = GetNode<HSplitContainer>("%HSplitContainer2");

        // Keep the ruler-row and track-row dividers in lockstep.
        // HSplitContainer2 has dragging_enabled = false, so only the ruler splitter
        // is interactive; we mirror its position down to the track row.
        _hSplitRuler.Dragged += offset => _hSplitTrack.SplitOffsets = [(int)offset];
    }

    /// <summary>
    /// Wire the document's <see cref="TimelineSetting"/> into all sub-controls.
    /// Must be called once after this panel is added to the tree, before <see cref="BindPlayhead"/>.
    /// </summary>
    public TimelinePanel BindTimeline(TimelineSetting setting, ReactiveProperty<int> currentFrame)
    {
        _zoomScrollBar.Setup(setting);
        
        _ruler.Observe(setting.PixelsPerFrame, setting.ScrollOffsetPixels, setting.FrameRate);
        _ruler.BindPlaybackRange(setting.PlaybackStart, setting.PlaybackEnd);
        _ruler.BindCurrentFrame(currentFrame);
        
        _bgGrid.Observe(setting.PixelsPerFrame, setting.ScrollOffsetPixels);

        // Start bar: green line at left edge of PlaybackStart
        _startBar.Observe(setting.PixelsPerFrame, setting.ScrollOffsetPixels, setting.PlaybackStart, _bgGrid);

        // End bar: red line at left edge of PlaybackEnd (exclusive boundary)
        _endBar.LineColor = new Color(0.9f, 0.25f, 0.25f, 0.9f);
        _endBar.Observe(setting.PixelsPerFrame, setting.ScrollOffsetPixels, setting.PlaybackEnd, _bgGrid);

        // FrameRate SpinBox
        _frameRateSpinBox.BindNumber(setting.FrameRate);
        
        _playhead.Observe(setting.PixelsPerFrame, setting.ScrollOffsetPixels, currentFrame, _bgGrid);

        return this;
    }
}