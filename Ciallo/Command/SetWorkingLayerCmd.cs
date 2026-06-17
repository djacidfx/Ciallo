using Ciallo.Data;
using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.GuiControl;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class SetWorkingLayerCmd : CommandBase
{
    private readonly bool _recordCelSelectionPreference;
    private Entity _celSelectionPreferenceFolder = Entity.Null;
    private ImmutableArray<int> _oldPreferredPath;
    private ImmutableArray<int> _newPreferredPath;
    public Entity OldLayerE;

    public SetWorkingLayerCmd(bool recordCelSelectionPreference = false)
    {
        _recordCelSelectionPreference = recordCelSelectionPreference;
    }

    public override void BeforeFirstDo(Entity newLayerE)
    {
        var sm = Document.Get<SelectionManager>();
        OldLayerE = sm.WorkingLayer.Value;

        if (!_recordCelSelectionPreference)
            return;

        if (!TryGetCelSelectionPreferenceTarget(newLayerE, out _celSelectionPreferenceFolder, out _newPreferredPath))
            return;

        _oldPreferredPath = _celSelectionPreferenceFolder
            .Get<FolderLayerSetting>().PreferredWorkingLayerPathForCelSelection.Value;
    }

    public override void Do(Entity newLayerE)
    {
        SetCelSelectionPreferencePath(_newPreferredPath);

        // Selection manager
        var sm = Document.Get<SelectionManager>();
        sm.WorkingLayer.Value = newLayerE;

        // Layer panel
        var layerTree = Document.Get<LayerTree>();
        layerTree.SetWorkingLayerNoSignal(newLayerE);

        // Timeline panel
        var trackTree = Document.Get<TrackTree>();
        trackTree.SetWorkingLayerNoSignal(newLayerE);
    }

    public override void Undo(Entity newLayerE)
    {
        var trackTree = Document.Get<TrackTree>();
        trackTree.SetWorkingLayerNoSignal(OldLayerE);

        // Layer panel
        var layerTree = Document.Get<LayerTree>();
        layerTree.SetWorkingLayerNoSignal(OldLayerE);

        // Selection manager
        var sm = Document.Get<SelectionManager>();
        sm.WorkingLayer.Value = OldLayerE;

        SetCelSelectionPreferencePath(_oldPreferredPath);
    }

    private void SetCelSelectionPreferencePath(ImmutableArray<int> path)
    {
        if (!_recordCelSelectionPreference
            || _celSelectionPreferenceFolder.IsNull
            || !_celSelectionPreferenceFolder.IsAlive)
            return;

        _celSelectionPreferenceFolder
            .Get<FolderLayerSetting>()
            .PreferredWorkingLayerPathForCelSelection
            .Value = path;
    }

    private static bool TryGetCelSelectionPreferenceTarget(
        Entity newLayerE,
        out Entity celFolder,
        out ImmutableArray<int> path)
    {
        celFolder = Entity.Null;
        path = [];

        if (newLayerE.IsNull || newLayerE.IsDocument || !newLayerE.IsAlive)
            return false;

        if (newLayerE.TryGet<FolderLayerSetting>()?.IsCelFolder == true)
            return false;

        var cursor = newLayerE;
        Entity exposedCel = Entity.Null;
        while (!cursor.IsNull && !cursor.IsDocument)
        {
            var parent = cursor.Get<LayerTreeNode>().ParentValue;
            if (parent.IsNull) break;

            if (parent.TryGet<FolderLayerSetting>()?.IsCelFolder == true)
            {
                celFolder = parent;
                exposedCel = cursor;
                break;
            }

            cursor = parent;
        }

        if (celFolder.IsNull || exposedCel.IsNull)
            return false;

        if (newLayerE == exposedCel)
            return true;

        EntityTreeNode<LayerTreeNode>.BreadthFirstSearch(
            exposedCel.Get<LayerTreeNode>(),
            newLayerE.Get<LayerTreeNode>(),
            out List<int> relativePath);
        if (relativePath == null)
            return false;

        path = [.. relativePath];
        return true;
    }
}
