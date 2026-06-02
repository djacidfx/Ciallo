using System.Collections.Generic;
using Ciallo.Command;
using Ciallo.Data;
using Frent;
using Godot;

namespace Ciallo.GuiControl;

/// <summary>
/// Shared right-click context menu for all <see cref="CelTrack"/> instances.
/// Inherits <see cref="PopupMenu"/> directly — one node in <c>TimelinePanel.tscn</c>.
///
/// Usage:
///   1. Call <see cref="InitDocument"/> once when a document is opened.
///   2. Call <see cref="Popup"/> from a <see cref="CelTrack"/> on right-click.
///
/// Whether the click is "on a cel" is determined internally from the exposure map.
/// </summary>
public partial class CelTrackRightClickMenu : PopupMenu
{
    // ── Document-level state ──────────────────────────────────────────────────
    private SelectionManager _selectionManager;

    // ── Context captured at Show() ────────────────────────────────────────────
    private Entity _celFolderEntity;
    private int _rightClickedFrame;
    private bool _onCel;

    // Ordered list of entities shown as cel-list items
    private readonly List<Entity> _celListEntities = new();

    // ── Menu item IDs ─────────────────────────────────────────────────────────
    private const int IdNewAnimationCel = 0;
    private const int IdDeleteCel = 1;
    private const int IdInsertFrame = 2;
    private const int IdDeleteFrame = 3;
    private const int CelListIdBase = 100;

    // ── Init ─────────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        IdPressed += OnMenuSelected;
    }

    /// <summary>
    /// Call once after a document is opened to cache document-level singletons.
    /// </summary>
    public void InitDocument(Entity document)
    {
        _selectionManager = document.Get<SelectionManager>();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Populates and displays the context menu.
    /// Whether the clicked frame has an existing cel is resolved from the exposure map.
    /// </summary>
    public void Popup(Entity celFolderEntity, int frame)
    {
        _celFolderEntity = celFolderEntity;
        _rightClickedFrame = frame;
        var exposures = celFolderEntity.Get<FolderLayerSetting>().Exposures;
        _onCel = exposures.ContainsKey(frame);

        RebuildMenu();

        Position = DisplayServer.MouseGetPosition();
        base.Popup();
    }

    // ── Menu building ─────────────────────────────────────────────────────────

    private void RebuildMenu()
    {
        Clear();
        _celListEntities.Clear();

        var children = _celFolderEntity.Get<LayerTreeNode>().Children;
        foreach (var celEntity in children)
        {
            if (celEntity.IsNull || !celEntity.IsAlive || !celEntity.Has<CommonLayerSetting>())
                continue;
            _celListEntities.Add(celEntity);
        }

        AddItem("New Animation Cel", IdNewAnimationCel);

        AddSeparator();

        string celListLabel = _onCel ? "Replace Cel:" : "Insert Cel:";
        AddItem(celListLabel, -1);
        SetItemDisabled(ItemCount - 1, true);

        if (_celListEntities.Count == 0)
        {
            AddItem("  (no cels)", -2);
            SetItemDisabled(ItemCount - 1, true);
        }
        else
        {
            for (int i = 0; i < _celListEntities.Count; i++)
            {
                string name = _celListEntities[i].Get<CommonLayerSetting>().Name.Value;
                AddItem("  " + (string.IsNullOrEmpty(name) ? "(unnamed)" : name), CelListIdBase + i);
            }
        }

        if (_onCel)
        {
            AddSeparator();
            AddItem("Delete Cel", IdDeleteCel);
        }

        AddSeparator();
        AddItem("Insert Frame", IdInsertFrame);
        AddItem("Delete Frame", IdDeleteFrame);
    }

    // ── Event handler ─────────────────────────────────────────────────────────

    private void OnMenuSelected(long id)
    {
        int intId = (int)id;
        switch (intId)
        {
            case IdNewAnimationCel:
                ActionNewAnimationCel();
                break;
            case IdDeleteCel:
                ActionDeleteCel();
                break;
            case IdInsertFrame:
                ActionInsertFrame();
                break;
            case IdDeleteFrame:
                ActionDeleteFrame();
                break;
            default:
                if (intId >= CelListIdBase)
                {
                    int idx = intId - CelListIdBase;
                    if (idx < _celListEntities.Count)
                        ActionInsertOrReplaceCel(_celListEntities[idx]);
                }
                break;
        }
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    private void ActionNewAnimationCel()
    {
        var exposures = _celFolderEntity.Get<FolderLayerSetting>().Exposures;
        int targetFrame;
        string name;

        if (_onCel)
        {
            (targetFrame, name) = TimelineAction.GetNewAnimationCelFrameName(
                _celFolderEntity, _rightClickedFrame);
        }
        else
        {
            targetFrame = exposures.ContainsKey(_rightClickedFrame)
                ? TimelineAction.FindNearestUnoccupiedFrame(exposures, _rightClickedFrame)
                : _rightClickedFrame;
            var usedNames = TimelineAction.GetUsedCelNames(_celFolderEntity);
            name = TimelineAction.GetNewAnimationCelName(exposures, targetFrame, usedNames);
        }

        var document = _celFolderEntity.Document;
        var cel = document.World.Create();

        new CommandBuilder(cel)
            .NewShapeLayer()
            .SetProperty(e => e.Get<CommonLayerSetting>().Name, name)
            .AddToLayerTree(_celFolderEntity)
            .SetWorkingLayer()
            .SetTarget(_celFolderEntity)
            .SetObservableCollection(
                e => e.Get<FolderLayerSetting>().Exposures,
                exp => exp.Add(targetFrame, cel))
            .SetTarget(document)
            .SetProperty(e => e.Get<SelectionManager>().CurrentFrame, targetFrame)
            .Commit();
    }

    private void ActionInsertOrReplaceCel(Entity celEntity)
    {
        var exposures = _celFolderEntity.Get<FolderLayerSetting>().Exposures;
        int frame = _rightClickedFrame;
        bool onCel = _onCel;
        string label = onCel ? "Replace Cel" : "Insert Cel";

        new CommandBuilder(label)
            .SetObservableCollection(exposures, exp =>
            {
                if (onCel && exp.ContainsKey(frame))
                    exp.Remove(frame);
                exp.Add(frame, celEntity);
            })
            .Commit();
    }

    private void ActionDeleteCel()
    {
        var exposures = _celFolderEntity.Get<FolderLayerSetting>().Exposures;
        int frame = _rightClickedFrame;
        if (!exposures.ContainsKey(frame)) return;

        new CommandBuilder("Delete Cel")
            .SetObservableCollection(exposures, exp => exp.Remove(frame))
            .Commit();
    }

    private void ActionInsertFrame()
    {
        const int frameCount = 1;
        int frame = _rightClickedFrame;
        var exposures = _celFolderEntity.Get<FolderLayerSetting>().Exposures;
        if (!TimelineFrameRetiming.InsertFramesWouldChange(exposures, frame, frameCount))
            return;

        new CommandBuilder("Insert Frame")
            .SetObservableCollection(exposures,
                exp => TimelineFrameRetiming.InsertFrames(exp, frame, frameCount))
            .Commit();
    }

    private void ActionDeleteFrame()
    {
        const int frameCount = 1;
        int frame = _rightClickedFrame;
        var exposures = _celFolderEntity.Get<FolderLayerSetting>().Exposures;
        if (!TimelineFrameRetiming.DeleteFramesWouldChange(exposures, frame, frameCount))
            return;

        new CommandBuilder("Delete Frame")
            .SetObservableCollection(exposures,
                exp => TimelineFrameRetiming.DeleteFrames(exp, frame, frameCount))
            .Commit();
    }
}
