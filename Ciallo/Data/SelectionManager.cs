using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Frent;
using ObservableCollections;
using R3;
using Godot;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class SelectionManager
{
    /// <summary>Current playhead position.</summary>
    [DataMember] public ReactiveProperty<int> CurrentFrame = new(1);

    [DataMember] public ObservableList<Entity> SelectedLayers = [];

    // Note: although current frame sync working layer on user side, the two properties are not directly synced on Data side.
    // The logics are implemented here but called by corresponding GUI control side.
    // Which make sure everything works OK even though CurrentFrame and WorkingLayer are not in sync.
    [DataMember] public ReactiveProperty<Entity> WorkingLayer = new(Entity.Null);
    public ReadOnlyReactiveProperty<Entity> WorkingCelFolder;

    [DataMember] public ReactiveProperty<Entity> WorkingStrokeBrush = new(Entity.Null);

    [DataMember] public ReactiveProperty<Entity> WorkingVectorFillBrush = new(Entity.Null);

    public ObservableList<Entity> SelectedShapes = [];

    public SelectionManager()
    {
        WorkingCelFolder = WorkingLayer.Select(layerE =>
        {
            if (layerE.IsNull || layerE.IsDocument)
                return Entity.Null;
            if (layerE.TryGet<FolderLayerSetting>()?.IsCel == true)
            {
                return layerE;
            }

            var ancestors = layerE.Get<LayerTreeNode>().EnumerateAncestors();
            foreach (Entity e in ancestors)
            {
                // Layer's parent must have FolderLayerSetting component, but it may not be a cel folder.
                if (e.Get<FolderLayerSetting>().IsCel) return e;
            }

            return Entity.Null;
        }).ToReadOnlyReactiveProperty();
    }

    /// <summary>
    /// Returns the entity to switch <see cref="WorkingLayer"/> to after the current frame moves
    /// from <paramref name="oldFrame"/> to <paramref name="newFrame"/>.
    /// <list type="bullet">
    /// <item>If <see cref="WorkingCelFolder"/> is null, returns <see cref="Entity.Null"/> (no switch).</item>
    /// <item>Otherwise finds the cel exposed at the new frame inside the cel folder.</item>
    /// <item>If the exposed cel is not a folder layer, returns it directly.</item>
    /// <item>If it is a folder layer, computes the relative index path from the previously
    ///   working layer down from its old exposed cel, then follows that path inside the new
    ///   exposed cel. Trims the path from the end until a valid node is found; falls back to
    ///   the exposed cel itself if nothing matches.</item>
    /// <item>Returns <see cref="Entity.Null"/> when no switch is needed (already on the target layer).</item>
    /// </list>
    /// </summary>
    public Entity ComputeWorkingLayerForSwitchingFrame(int oldFrame, int newFrame)
    {
        var celFolder = WorkingCelFolder.CurrentValue;
        if (celFolder.IsNull) return Entity.Null;

        var exposures = celFolder.Get<FolderLayerSetting>().Exposures;
        if (exposures == null) return Entity.Null;

        // Find exposed cel at new frame.
        int newFloor = exposures.FloorIndex(newFrame);
        if (newFloor < 0) return Entity.Null;
        var newExposedCel = exposures.GetValueAtIndex(newFloor);
        if (newExposedCel.IsNull || !newExposedCel.IsAlive) return Entity.Null;

        Entity result;

        if (!newExposedCel.Has<FolderLayerSetting>())
        {
            // Non-folder cel: switch directly to the cel.
            result = newExposedCel;
        }
        else
        {
            // Folder cel: try to preserve the relative path within the subtree.
            var oldWorkingLayer = WorkingLayer.Value;

            // Find the cel that was exposed at the old frame.
            int oldFloor = exposures.FloorIndex(oldFrame);
            Entity oldExposedCel = oldFloor >= 0 ? exposures.GetValueAtIndex(oldFloor) : Entity.Null;

            // Compute index path from oldExposedCel down to oldWorkingLayer.
            List<int> relativePath = null;
            if (!oldExposedCel.IsNull && oldExposedCel.IsAlive
                && !oldWorkingLayer.IsNull && oldWorkingLayer.IsAlive
                && oldWorkingLayer != oldExposedCel)
            {
                EntityTreeNode<LayerTreeNode>.BreadthFirstSearch(
                    oldExposedCel.Get<LayerTreeNode>(),
                    oldWorkingLayer.Get<LayerTreeNode>(),
                    out relativePath);
            }
            relativePath ??= [];

            // Try the path in the new cel, trimming from the end until a node is found.
            var newCelNode = newExposedCel.Get<LayerTreeNode>();
            result = newExposedCel;
            for (int len = relativePath.Count; len > 0; len--)
            {
                var node = newCelNode.GetNodeOrNull(relativePath.Take(len).ToArray());
                if (node != null)
                {
                    result = node.Self;
                    break;
                }
            }
        }

        // Return Null when no switch is actually needed.
        return result == WorkingLayer.Value ? Entity.Null : result;
    }

    /// <summary>
    /// Returns the frame index to switch to after switching working layer.
    /// <list type="bullet">
    /// <item>If <paramref name="newWorkingLayer"/> is null, the document, outside any cel folder,
    ///   or is itself a cel folder root, returns the current frame (no switch).</item>
    /// <item>Otherwise finds the direct cel under the nearest cel folder ancestor and searches all
    ///   exposure ranges that show that cel.</item>
    /// <item>If the current frame already lies inside one of those ranges, returns that range's start frame.</item>
    /// <item>Otherwise returns the nearest matching range start frame. Ties prefer the earlier frame.</item>
    /// <item>If the layer is never exposed, returns the current frame.</item>
    /// </list>
    /// </summary>
    public int ComputeFrameForSwitchingWorkingLayer(Entity newWorkingLayer)
    {
        int currentFrame = CurrentFrame.Value;
        if (newWorkingLayer.IsNull || newWorkingLayer.IsDocument || !newWorkingLayer.IsAlive)
            return currentFrame;

        if (newWorkingLayer.TryGet<FolderLayerSetting>()?.IsCel == true)
            return currentFrame;

        Entity celFolder = Entity.Null;
        Entity targetCel = Entity.Null;
        var cursor = newWorkingLayer;

        while (!cursor.IsNull && !cursor.IsDocument)
        {
            var parent = cursor.Get<LayerTreeNode>().ParentValue;
            if (parent.IsNull) break;

            if (parent.TryGet<FolderLayerSetting>()?.IsCel == true)
            {
                celFolder = parent;
                targetCel = cursor;
                break;
            }

            cursor = parent;
        }

        if (celFolder.IsNull || targetCel.IsNull) return currentFrame;

        var exposures = celFolder.Get<FolderLayerSetting>().Exposures;
        if (exposures == null || exposures.Count == 0) return currentFrame;

        int bestFrame = currentFrame;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < exposures.Count; i++)
        {
            if (exposures.GetValueAtIndex(i) != targetCel) continue;

            int start = exposures.GetKeyAtIndex(i);
            int endExclusive = i + 1 < exposures.Count ? exposures.GetKeyAtIndex(i + 1) : int.MaxValue;

            if (currentFrame >= start && currentFrame < endExclusive)
                return start;

            int candidate = start;
            int distance = Mathf.Abs(candidate - currentFrame);
            if (distance < bestDistance || (distance == bestDistance && candidate < bestFrame))
            {
                bestFrame = candidate;
                bestDistance = distance;
            }
        }

        return bestDistance == int.MaxValue ? currentFrame : bestFrame;
    }
}
