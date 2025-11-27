using System;
using Frent;

namespace Ciallo.Data;
/*
- When an entity is tagged with `ToSerializeTag`, it will be serialized.
- When its components are attributed with [ToSerialize] (and also [DataContract] when necessary), these components are serialized.
- An entity without `ToSerializeTag` but has [ToSerialize] component won't be serialized, including entity itself and its components.
*/

/// <summary>
/// Label a component should be serialized by MessagePack
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class ToSerializeAttribute : Attribute;

/// <summary>
/// Label an entity should be serialized by MessagePack
/// </summary>
public struct ToSerializeTag;

public static class EntityExtension
{
    /// <summary>
    /// Entity has been deleted by user or null.
    /// </summary>
    /// <remarks>
    /// It may be deleted by undo stack or not.
    /// </remarks>
    public static bool IsDeletedOrNull(this Entity self)
    {
        return !self.IsAlive || !self.Tagged<ToSerializeTag>();
    }
}