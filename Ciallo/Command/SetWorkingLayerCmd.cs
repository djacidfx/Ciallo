using Ciallo.Data;
using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.GuiControl;
using Frent;

namespace Ciallo.Command;

[CommandBuilder]
public class SetWorkingLayerCmd : CommandBase
{
    private readonly bool _updatePreferredWorkingLayerPathOnRollingFrame;
    private Entity _preferredCelFolder = Entity.Null;
    private ImmutableArray<int> _oldPreferredPath;
    private ImmutableArray<int> _newPreferredPath;
    public Entity OldLayerE;

    public SetWorkingLayerCmd(bool updatePreferredWorkingLayerPathOnRollingFrame = false)
    {
        _updatePreferredWorkingLayerPathOnRollingFrame = updatePreferredWorkingLayerPathOnRollingFrame;
    }

    public override void BeforeFirstDo(Entity newLayerE)
    {
        var sm = Document.Get<SelectionManager>();
        OldLayerE = sm.WorkingLayer.Value;

        if (!_updatePreferredWorkingLayerPathOnRollingFrame)
            return;

        if (!TryGetPreferredPathTarget(newLayerE, out _preferredCelFolder, out _newPreferredPath))
            return;

        _oldPreferredPath = _preferredCelFolder
            .Get<FolderLayerSetting>()
            .PreferredWorkingLayerPathOnRollingFrame
            .Value;
    }

    public override void Do(Entity newLayerE)
    {
        SetPreferredPath(_newPreferredPath);

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

        SetPreferredPath(_oldPreferredPath);
    }

    private void SetPreferredPath(ImmutableArray<int> path)
    {
        if (!_updatePreferredWorkingLayerPathOnRollingFrame
            || _preferredCelFolder.IsNull
            || !_preferredCelFolder.IsAlive)
            return;

        _preferredCelFolder
            .Get<FolderLayerSetting>()
            .PreferredWorkingLayerPathOnRollingFrame
            .Value = path;
    }

    private static bool TryGetPreferredPathTarget(
        Entity newLayerE,
        out Entity celFolder,
        out ImmutableArray<int> path)
    {
        celFolder = Entity.Null;
        path = [];

        if (newLayerE.IsNull || newLayerE.IsDocument || !newLayerE.IsAlive)
            return false;

        if (newLayerE.TryGet<FolderLayerSetting>()?.IsCel == true)
            return false;

        var cursor = newLayerE;
        Entity exposedCel = Entity.Null;
        while (!cursor.IsNull && !cursor.IsDocument)
        {
            var parent = cursor.Get<LayerTreeNode>().ParentValue;
            if (parent.IsNull) break;

            if (parent.TryGet<FolderLayerSetting>()?.IsCel == true)
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
