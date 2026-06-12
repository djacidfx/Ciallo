using System.Collections.Immutable;
using Ciallo.Data;
using Frent;
using Godot;

namespace Ciallo.Command;

[CommandBuilder]
public class SetPolylineGeometryCmd : CommandBase
{
    public ImmutableArray<Vector2>? Positions { get; }
    public ImmutableArray<float>? Radii { get; }
    public ImmutableArray<float>? Pressures { get; }
    public ImmutableArray<Vector2>? Tilts { get; }

    private CommandBuilder _cmd;

    public SetPolylineGeometryCmd(
        ImmutableArray<Vector2>? positions = null,
        ImmutableArray<float>? radii = null,
        ImmutableArray<float>? pressures = null,
        ImmutableArray<Vector2>? tilts = null)
    {
        Positions = positions;
        Radii = radii;
        Pressures = pressures;
        Tilts = tilts;
    }

    public override void BeforeFirstDo(Entity targetE)
    {
        _cmd = new(targetE);
        if (Positions.HasValue)
            _cmd.SetProperty(e => e.Get<PolylineGeometry>().Positions, Positions.Value);
        if (Radii.HasValue)
            _cmd.SetProperty(e => e.Get<PolylineGeometry>().Radii, Radii.Value);
        if (Pressures.HasValue)
            _cmd.SetProperty(e => e.Get<PolylineGeometry>().Pressures, Pressures.Value);
        if (Tilts.HasValue)
            _cmd.SetProperty(e => e.Get<PolylineGeometry>().Tilts, Tilts.Value);
    }

    public override void Do(Entity targetE)
    {
        _cmd.Do();
    }

    public override void Undo(Entity targetE)
    {
        _cmd.Undo();
    }
}