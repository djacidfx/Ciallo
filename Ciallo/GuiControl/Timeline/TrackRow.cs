using Ciallo.Data;
using Frent;
using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// A full-width timeline track row.
/// Left panel: <see cref="TrackHeaderBlock"/> (always visible).
/// Right panel: <see cref="CelTrack"/> (only for CelFolder layers) or an empty placeholder.
/// Split offset is kept in sync with the HSplitRuler by <see cref="TrackTree"/>.
/// </summary>
[SceneTree, Instantiable]
public partial class TrackRow : HSplitContainer
{
    public override void _Ready()
    {
        ApplyThemeOverrides();
    }

    private void ApplyThemeOverrides()
    {
        AddThemeStyleboxOverride("split_bar_background", new StyleBoxEmpty());
    }

    public void Configure(int splitOffset, TrackRowWrapper wrapper)
    {
        DraggingEnabled = false;
        SplitOffsets = [splitOffset];
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        HeaderBlock.OwningWrapper = wrapper;
        CelTrack.Visible = false;
    }

    public CelTrack EnableCelTrack(
        Entity layerE,
        CelTrackRightClickMenu rightClickMenu,
        CompositeDisposable subs)
    {
        var timeSetting = layerE.Document.Get<TimelineSetting>();
        var folderSetting = layerE.Get<FolderLayerSetting>();
        var selectionManager = layerE.Document.Get<SelectionManager>();
        CelTrack.Visible = true;
        CelTrack.Observe(
            timeSetting.PixelsPerFrame,
            timeSetting.ScrollOffsetFrame,
            timeSetting.PlaybackStart,
            timeSetting.PlaybackEnd,
            subs);
        CelTrack.Bind(layerE, folderSetting.Exposures, selectionManager, subs);
        CelTrack.RightClickMenu = rightClickMenu;
        return CelTrack;
    }
}
