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
        // This is not worth the implementation and migration cost until Ciallo needs serious
        // version control, cross-program project editing, or stable external tooling.
        // 
        // June 22, 2026. We implement this but is it worthy? Opaque blobs are everywhere limited by the database.
        // And we have no real use for it yet. 
    }

    public override void _Notification(int what) { }

    public override void _Ready() { }

    public override void _ExitTree()
    {

    }
}
