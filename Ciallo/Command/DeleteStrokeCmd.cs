using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Command;

[CommandBuilder]
public class DeleteStrokeCmd : CommandBase
{
    private StrokeView _strokeView;
    private PolylineWireframe _strokeOverlay;
    private Body _strokeBody;

    public override IEnumerable<Entity> UndoRefEntities => ToEnumerable(TargetE);
    public override IEnumerable<GodotObject> UndoRefObjects => new List<GodotObject> { _strokeView, _strokeOverlay, _strokeBody };

    public override void BeforeFirstDo(Entity strokeE)
    {
        _strokeBody = strokeE.Get<Body>();
        _strokeOverlay = strokeE.Get<PolylineWireframe>();
        _strokeView = strokeE.Get<StrokeView>();
    }

    public override void Do(Entity strokeE)
    {
        // Selection manager
        Document.Get<SelectionManager>().SelectedPolylines.Remove(strokeE);

        // Body
        strokeE.Remove<Body>();

        // Overlay
        strokeE.Remove<PolylineWireframe>();

        // View
        strokeE.Remove<StrokeView>();

        // Data
        strokeE.Detach<ToSerializeTag>();
    }

    public override void Undo(Entity strokeE)
    {
        // Data
        strokeE.Tag<ToSerializeTag>();

        // View
        strokeE.Add(_strokeView);

        // Overlay
        strokeE.Add(_strokeOverlay);

        // Body
        strokeE.Add(_strokeBody);
    }
}