using System;

namespace Ciallo.Data;
/*
- When an entity is tagged with `ToSerializeTag`, it will be serialized.
- When its components are attributed with [ToSerialize], these components are serialized.
- An entity without `ToSerializeTag` but has [ToSerialize] component won't be serialized, including entity itself and its components.
*/

/// <summary>
/// Label a component should be serialized by project persistence.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class ToSerializeAttribute : Attribute
{
    public string Name { get; }

    public ToSerializeAttribute() { }

    public ToSerializeAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Label an entity should be serialized by project persistence.
/// </summary>
public struct ToSerializeTag;
