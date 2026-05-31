using Godot;
using System.Runtime;
using MessagePack;
using MessagePackGodot;
using MessagePack.Resolvers;

namespace Ciallo;

public partial class AutoloadMisc : Node
{
    public override void _EnterTree()
    {
        // Hopefully this can reduce GC spikes.
        GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

        // Message pack serializer setup
        var defaultResolver = CompositeResolver.Create(
            [
                EntityToIndexFormatter.Instance,
                TypeFormatter.Instance,
                ImageTextureFormatter.Instance,
                ImageFormatter.Instance
            ],
            [
                GodotResolver.Instance,
                AttributeFormatterResolver.Instance,
                ReactivePropertyResolver.Instance,
                StandardResolverAllowPrivate.Instance
            ]
        );
        MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(defaultResolver);

        // May 30, 2026. Failed experiment: store Frent's ECS world in a database.
        // The ECS model maps naturally to relational storage at the schema level:
        // - Entity ids can be primary keys.
        // - Each component type can be a table.
        // - Component fields can be columns.
        // - Entity-valued fields can be foreign keys to the entity table.
        //
        // The main goal is not powerful relational querying. It is a file format that is
        // easy for other programs to inspect: they should quickly see which entities have
        // which components and which fields exist, even if some field values are opaque blobs.
        //
        // The hard part is not the database itself, but the impedance mismatch between our
        // data components and database mappers such as EF Core, Dapper, sqlite-net:
        // - ReactiveProperty<T> is persistence noise. We want to store its Value, not the wrapper.
        // - Generic ReactiveProperty<T> handling requires custom mapping, converters, or DTOs.
        // - Collections can be mapped, but every choice has a cost: child tables, JSON/blob columns,
        //   or custom converters all make schema evolution and partial updates harder.
        // - Godot types and Frent Entity handles need stable serialized representations.
        // - Fields that are too awkward for database mapping would still need MessagePack blobs.
        //
        // A hybrid schema is possible: keep entity/component/field structure in database columns,
        // and serialize difficult values as byte[] with MessagePack. This preserves inspectability
        // at the ECS/component/field level, but blob fields lose database-level type information,
        // constraints, querying, diffing, and migration support.
        //
        // This is not worth the implementation and migration cost until Ciallo needs serious
        // version control, cross-program project editing, or stable external tooling.
    }

    public override void _Notification(int what) { }

    public override void _Ready() { }

    public override void _ExitTree()
    {

    }
}
