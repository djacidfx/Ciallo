using System;
using MessagePack;

namespace Ciallo.Data;
/*
When an entity is tagged with `ToSerializeTag`, it will be serialized.
When its components are attributed with [ToSerialize] (and also [MemoryPackObject] when necessary). these components are serialized
An entity without `ToSerializeTag` but has [ToSerialize] component won't be serialized at all, entity itself and its components.
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