using Godot;

namespace Ciallo.Widget;

internal static class DockableSignalConnection
{
    // Godot owns signal connections natively, so they can outlive a managed tool-script instance.
    // Centralizing idempotent binding keeps reload and tree lifecycle callbacks composable.
    public static void Rebind(
        GodotObject previousSource,
        GodotObject currentSource,
        StringName signal,
        Callable callable)
    {
        if (GodotObject.IsInstanceValid(previousSource) && previousSource != currentSource)
            Disconnect(previousSource, signal, callable);

        EnsureConnected(currentSource, signal, callable);
    }

    public static void EnsureConnected(GodotObject source, StringName signal, Callable callable)
    {
        if (!GodotObject.IsInstanceValid(source) || source.IsConnected(signal, callable))
            return;

        source.Connect(signal, callable);
    }

    public static void Disconnect(GodotObject source, StringName signal, Callable callable)
    {
        if (!GodotObject.IsInstanceValid(source) || !source.IsConnected(signal, callable))
            return;

        source.Disconnect(signal, callable);
    }
}
