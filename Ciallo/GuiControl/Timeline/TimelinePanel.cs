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
    public override void _Ready()
    {
        TrackTree.RightClickMenu = CelTrackRightClickMenu;

        // Keep all track-row splits, the scrollbar spacer, and the BackgroundGrid in lockstep.
        HSplitRuler.Dragged += offset =>
        {
            TrackTree.SplitOffset = (int)offset;
            HSplitScrollBar.SplitOffsets = [(int)offset];
            HSplitBgGrid.SplitOffsets = [(int)offset];
        };
    }

    public void Init(Entity document)
    {
        var setting = document.Get<TimelineSetting>();
        var selectionManager = document.Get<SelectionManager>();

        BindTimeline(setting, selectionManager.CurrentFrame);
        InitTrackTree(document, selectionManager);
        TimelineAction.Init(document);
    }

    /// <summary>
    /// Wire the document's <see cref="TimelineSetting"/> into all sub-controls.
    /// </summary>
    private void BindTimeline(TimelineSetting setting, ReactiveProperty<int> currentFrame)
    {
        ZoomableHScrollBar.Setup(setting);

        TimelineRuler.Observe(setting.PixelsPerFrame, setting.ScrollOffsetFrame, setting.FrameRate);
        TimelineRuler.BindPlaybackRange(setting.PlaybackStart, setting.PlaybackEnd);
        TimelineRuler.BindCurrentFrame(currentFrame);
        TimelineRuler.BindPlayhead(Playhead);

        BackgroundGrid.Observe(setting.PixelsPerFrame, setting.ScrollOffsetFrame);
        BackgroundGrid.BindPlaybackRange(setting.PlaybackStart, setting.PlaybackEnd);

        // Start bar: green line + left-handle at PlaybackStart frame
        PlaybackStartBar.IsStart = true;
        PlaybackStartBar.Observe(setting.PixelsPerFrame, setting.ScrollOffsetFrame, setting.PlaybackStart);

        // End bar: red line + right-handle at PlaybackEnd frame
        PlaybackEndBar.IsStart = false;
        PlaybackEndBar.Observe(setting.PixelsPerFrame, setting.ScrollOffsetFrame, setting.PlaybackEnd);

        Playhead.Observe(setting.PixelsPerFrame, setting.ScrollOffsetFrame, currentFrame);
    }

    /// <summary>
    /// Registers <see cref="TrackTree"/>
    /// </summary>
    private void InitTrackTree(Entity document, SelectionManager selectionManager)
    {
        document.Add(TrackTree);
        document.Add(TrackTree.RootWrapper);
        document.Add(BackgroundGrid);
        TimelineRuler.BindSelectionManager(selectionManager);
        CelTrackRightClickMenu.InitDocument(document);
    }
}
