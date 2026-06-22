namespace Ciallo.Data;

/// <summary>
/// Marks a layer that is a direct child cel of a CelFolder.
/// </summary>
/// <remarks>
/// Non-persistent: this tag is never serialized. It is maintained solely by a CelFolder's
/// <c>ObserveAddChild</c>/<c>ObserveRemoveChild</c> subscriptions.
/// </remarks>
public struct CelTag;
