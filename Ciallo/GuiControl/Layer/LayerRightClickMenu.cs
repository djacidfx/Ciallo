using Ciallo.Data;
using Frent;
using Godot;

namespace Ciallo.GuiControl;

/// <summary>
/// Shared right-click context menu for layer header labels.
/// One node lives in each layer-tree scene and is shown only from LabelLineEdit right-clicks.
/// </summary>
public partial class LayerRightClickMenu : PopupMenu
{
    private Entity _targetLayer;
    private bool _showTimelineLayerActions;

    private const int IdNewShapeLayer = 0;
    private const int IdNewFolderLayer = 1;
    private const int IdNewCelFolderLayer = 2;
    private const int IdDeleteLayer = 3;
    private const int IdUngroupFolder = 4;

    public override void _Ready()
    {
        IdPressed += OnMenuSelected;
    }

    public void Popup(Entity targetLayer, bool showTimelineLayerActions)
    {
        _targetLayer = targetLayer;
        _showTimelineLayerActions = showTimelineLayerActions;

        RebuildMenu();

        Position = DisplayServer.MouseGetPosition();
        base.Popup();
    }

    private void RebuildMenu()
    {
        Clear();

        AddItem("New Shape Layer", IdNewShapeLayer);
        AddItem("New Folder Layer", IdNewFolderLayer);
        if (_showTimelineLayerActions)
            AddItem("New Cel Folder Layer", IdNewCelFolderLayer);

        AddSeparator();
        AddItem("Delete Layer", IdDeleteLayer);
        if (_targetLayer.Has<FolderLayerSetting>())
            AddItem("Ungroup Folder", IdUngroupFolder);
    }

    private void OnMenuSelected(long id)
    {
        if (_targetLayer.IsNull || !_targetLayer.IsAlive)
            return;

        switch ((int)id)
        {
            case IdNewShapeLayer:
                LayerContextActions.NewShapeLayer(_targetLayer);
                break;
            case IdNewFolderLayer:
                LayerContextActions.NewFolderLayer(_targetLayer);
                break;
            case IdNewCelFolderLayer:
                LayerContextActions.NewCelFolderLayer(_targetLayer);
                break;
            case IdDeleteLayer:
                LayerContextActions.DeleteLayer(_targetLayer);
                break;
            case IdUngroupFolder:
                LayerContextActions.UngroupFolder(_targetLayer);
                break;
        }
    }
}
