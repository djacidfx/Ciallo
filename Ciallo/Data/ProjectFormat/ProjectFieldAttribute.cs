using System;

namespace Ciallo.Data;

public enum StorageKind
{
    Auto,
    Entity,
    Blob,
}

// If entity itself can be null i.e. e = Entity.Null, not the field is nullable
public enum EntityNullability
{
    Nullable,
    Required,
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class ProjectFieldAttribute : Attribute
{
    public string Name { get; }
    public StorageKind StorageKind { get; }
    public EntityNullability EntityNullability { get; }

    public ProjectFieldAttribute()
        : this(null, StorageKind.Auto, EntityNullability.Nullable) { }

    public ProjectFieldAttribute(string name)
        : this(name, StorageKind.Auto, EntityNullability.Nullable) { }

    public ProjectFieldAttribute(StorageKind storageKind)
        : this(null, storageKind, EntityNullability.Nullable) { }

    public ProjectFieldAttribute(StorageKind storageKind, EntityNullability entityNullability)
        : this(null, storageKind, entityNullability) { }

    public ProjectFieldAttribute(string name, StorageKind storageKind)
        : this(name, storageKind, EntityNullability.Nullable) { }

    public ProjectFieldAttribute(string name, StorageKind storageKind, EntityNullability entityNullability)
    {
        Name = name;
        StorageKind = storageKind;
        EntityNullability = entityNullability;
    }
}
