using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Frent;
using ObservableCollections;
using R3;

namespace Ciallo.Data.ProjectFormat;

internal sealed class FieldDescriptor
{
    public ComponentDescriptor Component { get; }
    public FieldInfo Field { get; }
    public string Name { get; }
    public StorageKind StorageKind { get; }
    public EntityNullability EntityNullability { get; }
    public FieldShape Shape { get; }
    public Type FieldType { get; }
    public Type ValueType { get; }
    public Type NonNullableValueType { get; }
    public Type ElementType { get; }
    public bool IsReactive { get; }
    public bool IsNullable { get; }
    public string EntityColumnName => Name + "_entity_id";
    public string BlobColumnName => Name + "_blob";
    public string CountColumnName => Name + "_count";
    public string ExistsColumnName => Name + "_exists";
    public string ChildTableName => Component.TableName + "_" + Name;
    public bool IsChildTable => Shape is FieldShape.List or FieldShape.Set or FieldShape.IntKeyMap;
    public IReadOnlyList<ColumnDescriptor> MainColumns { get; }

    private FieldDescriptor(
        ComponentDescriptor component,
        FieldInfo field,
        ProjectFieldAttribute attr,
        FieldShape shape,
        Type fieldType,
        Type valueType,
        Type nonNullableValueType,
        Type elementType,
        bool isReactive,
        bool isNullable)
    {
        Component = component;
        Field = field;
        Name = string.IsNullOrWhiteSpace(attr.Name) ? field.Name : attr.Name;
        StorageKind = attr.StorageKind;
        EntityNullability = attr.EntityNullability;
        Shape = shape;
        FieldType = fieldType;
        ValueType = valueType;
        NonNullableValueType = nonNullableValueType;
        ElementType = elementType;
        IsReactive = isReactive;
        IsNullable = isNullable;
        MainColumns = BuildMainColumns();
    }

    public static FieldDescriptor TryCreate(ComponentDescriptor component, FieldInfo field)
    {
        var attr = field.GetCustomAttribute<ProjectFieldAttribute>();
        if (attr == null)
            return null;

        var fieldType = field.FieldType;
        var isReactive = IsReactiveProperty(fieldType);
        var valueType = isReactive ? fieldType.GetGenericArguments()[0] : fieldType;
        var nonNullableType = Nullable.GetUnderlyingType(valueType) ?? valueType;
        var isNullable = !valueType.IsValueType || Nullable.GetUnderlyingType(valueType) != null;
        var shape = ResolveShape(attr.StorageKind, nonNullableType);
        var elementType = shape == FieldShape.RawArray ? ResolveRawArrayElementType(nonNullableType) : null;

        return new FieldDescriptor(
            component,
            field,
            attr,
            shape,
            fieldType,
            valueType,
            nonNullableType,
            elementType,
            isReactive,
            isNullable);
    }

    public object GetProjectValue(object component)
    {
        var value = Field.GetValue(component);
        if (!IsReactive || value == null)
            return value;
        return FieldType.GetProperty("Value")!.GetValue(value);
    }

    public object GetFieldStorageObject(object component)
    {
        return Field.GetValue(component);
    }

    public void SetProjectValue(object component, object value)
    {
        if (IsReactive)
        {
            var property = Field.GetValue(component);
            if (property == null)
            {
                property = Activator.CreateInstance(FieldType, value);
                Field.SetValue(component, property);
                return;
            }
            FieldType.GetProperty("Value")!.SetValue(property, value);
            return;
        }

        Field.SetValue(component, value);
    }

    public void SetCollectionExists(object component, bool exists)
    {
        var current = Field.GetValue(component);
        if (!exists)
        {
            ClearEntityCollection(current);

            if (!Field.IsInitOnly)
                Field.SetValue(component, null);
            return;
        }

        if (current == null)
        {
            if (Field.IsInitOnly)
                throw new InvalidOperationException($"{Component.Name}.{Name} is readonly and null.");
            current = Activator.CreateInstance(FieldType)
                      ?? throw new InvalidOperationException($"Cannot create collection {FieldType}.");
            Field.SetValue(component, current);
        }

        if (ClearEntityCollection(current))
            return;

        throw new InvalidOperationException($"{Component.Name}.{Name} is not a supported entity collection.");
    }

    private static bool ClearEntityCollection(object value)
    {
        switch (value)
        {
            case null:
                return false;
            case ObservableList<Entity> observableList:
                observableList.Clear();
                return true;
            case ObservableHashSet<Entity> observableSet:
                observableSet.Clear();
                return true;
            case ObservableSortedList<int, Entity> observableMap:
                observableMap.Clear();
                return true;
            case ICollection<Entity> collection:
                collection.Clear();
                return true;
            case IDictionary<int, Entity> map:
                map.Clear();
                return true;
            default:
                return false;
        }
    }

    private IReadOnlyList<ColumnDescriptor> BuildMainColumns()
    {
        return Shape switch
        {
            FieldShape.Scalar => [new(Name, ScalarSqlType(NonNullableValueType))],
            FieldShape.Entity => [new(EntityColumnName, "integer")],
            FieldShape.Blob => [new(BlobColumnName, "blob")],
            FieldShape.RawArray => [new(BlobColumnName, "blob"), new(CountColumnName, "integer")],
            FieldShape.List or FieldShape.Set or FieldShape.IntKeyMap => [new(ExistsColumnName, "integer")],
            _ => throw new ArgumentOutOfRangeException(nameof(Shape), Shape, null)
        };
    }

    private static string ScalarSqlType(Type type)
    {
        if (type == typeof(string))
            return "text";
        if (type == typeof(float) || type == typeof(double))
            return "real";
        return "integer";
    }

    private static FieldShape ResolveShape(StorageKind storageKind, Type valueType)
    {
        return storageKind switch
        {
            StorageKind.Entity => ResolveEntityShape(valueType),
            StorageKind.RawArray => FieldShape.RawArray,
            StorageKind.Blob => FieldShape.Blob,
            StorageKind.Auto => IsScalar(valueType) ? FieldShape.Scalar : FieldShape.Blob,
            _ => throw new ArgumentOutOfRangeException(nameof(storageKind), storageKind, null)
        };
    }

    private static FieldShape ResolveEntityShape(Type valueType)
    {
        if (valueType == typeof(Entity))
            return FieldShape.Entity;

        if (!valueType.IsGenericType)
            throw new InvalidOperationException($"{valueType} is not a supported entity field.");

        var def = valueType.GetGenericTypeDefinition();
        var args = valueType.GetGenericArguments();
        if ((def == typeof(List<>) || def == typeof(ObservableList<>)) && args[0] == typeof(Entity))
            return FieldShape.List;
        if ((def == typeof(HashSet<>) || def == typeof(ObservableHashSet<>)) && args[0] == typeof(Entity))
            return FieldShape.Set;
        if ((def == typeof(SortedList<,>) || def == typeof(ObservableSortedList<,>)) &&
            args[0] == typeof(int) &&
            args[1] == typeof(Entity))
            return FieldShape.IntKeyMap;

        throw new InvalidOperationException($"{valueType} is not a supported entity field.");
    }

    private static Type ResolveRawArrayElementType(Type valueType)
    {
        Type elementType = null;
        if (valueType.IsArray)
            elementType = valueType.GetElementType();
        else if (valueType.IsGenericType)
        {
            var def = valueType.GetGenericTypeDefinition();
            if (def == typeof(ImmutableArray<>) || def == typeof(List<>))
                elementType = valueType.GetGenericArguments()[0];
        }

        if (elementType == null || !RawArrayCodec.IsSupportedElementType(elementType))
            throw new InvalidOperationException($"{valueType} is not a supported raw array field.");

        return elementType;
    }

    private static bool IsReactiveProperty(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ReactiveProperty<>);
    }

    private static bool IsScalar(Type type)
    {
        return type == typeof(string)
               || type == typeof(bool)
               || type.IsEnum
               || type == typeof(byte)
               || type == typeof(sbyte)
               || type == typeof(short)
               || type == typeof(ushort)
               || type == typeof(int)
               || type == typeof(uint)
               || type == typeof(long)
               || type == typeof(ulong)
               || type == typeof(float)
               || type == typeof(double);
    }
}

internal enum FieldShape
{
    Scalar,
    Entity,
    List,
    Set,
    IntKeyMap,
    RawArray,
    Blob,
}
