using System;

namespace Ciallo.Data;
/*
- ToSerializeTag marks a normal document entity.
- Normal document entities are serialized, participate in document-wide business queries,
  and are visible to user-facing document systems.
- Untagged entities could be generated from user delete object, user copy temp entity, and etc.
- Untagged entities may still live in a document world as transient/internal data,
  but document-wide business queries must ignore them.
- When a normal document entity has components attributed with [ToSerialize], those components are serialized.
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
/// Marks a normal document entity. Normal document entities are persisted and included in document-wide business queries.
/// </summary>
public struct ToSerializeTag;
