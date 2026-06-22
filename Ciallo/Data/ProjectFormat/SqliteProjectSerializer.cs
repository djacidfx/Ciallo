using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Frent;
using Godot;
using MessagePack;
using Microsoft.Data.Sqlite;
using ObservableCollections;
using SQLitePCL;

// Note: May 31, 2026. Fully AI gen with grill me (GPT5.5 asked Shen 50+ questions on this and took him one and half hours to answer)
// Shen already forget all about SQL

namespace Ciallo.Data.ProjectFormat;

public static class SqliteProjectSerializer
{
    public const int FormatVersion = 1;
    private const string ManifestPath = "manifest.json";
    private const string SqlitePath = "project.sqlite";

    static SqliteProjectSerializer()
    {
        Batteries_V2.Init();
    }

    #region Public API

    public static void Save(Entity document, string filePath)
    {
        var targetPath = Path.GetFullPath(filePath);
        var targetDirectory = Path.GetDirectoryName(targetPath)
                              ?? throw new InvalidOperationException($"File {filePath} has no directory.");
        Directory.CreateDirectory(targetDirectory);

        var tempRoot = Path.Combine(Path.GetTempPath(), "CialloSave_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var sqlitePath = Path.Combine(tempRoot, SqlitePath);
        var stagingPath = Path.Combine(
            targetDirectory,
            "." + Path.GetFileName(targetPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");

        try
        {
            var registry = ProjectFormatRegistry.Create();
            WriteSqlite(document, sqlitePath, registry);
            SqliteConnection.ClearAllPools();
            WriteZip(stagingPath, sqlitePath);
            CommitSave(stagingPath, targetPath);
        }
        finally
        {
            TryDeleteFile(stagingPath);
            try
            {
                Directory.Delete(tempRoot, true);
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }
    }

    private static void CommitSave(string stagingPath, string targetPath)
    {
        if (File.Exists(targetPath))
        {
            var backupPath = Path.Combine(
                Path.GetDirectoryName(targetPath)!,
                "." + Path.GetFileName(targetPath) + "." + Guid.NewGuid().ToString("N") + ".bak");
            File.Replace(stagingPath, targetPath, backupPath);
            TryDeleteFile(backupPath);
            return;
        }

        File.Move(stagingPath, targetPath, false);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    // Return Document entity
    public static Entity Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File {filePath} not found.");

        var tempRoot = Path.Combine(Path.GetTempPath(), "CialloLoad_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var sqlitePath = Path.Combine(tempRoot, SqlitePath);

        try
        {
            ReadZip(filePath, sqlitePath);
            var registry = ProjectFormatRegistry.Create();
            var document = ReadSqlite(sqlitePath, registry);
            document.Get<DocumentSetting>().FilePath.Value = filePath;
            return document;
        }
        finally
        {
            try
            {
                Directory.Delete(tempRoot, true);
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }
    }

    #endregion

    #region Zip I/O

    private static void WriteZip(string zipPath, string sqlitePath)
    {
        var manifest = $$"""
                       {
                         "format": "ciallo-project",
                         "formatVersion": {{FormatVersion}},
                         "sqlitePath": "{{SqlitePath}}"
                       }
                       """;

        var writer = new ZipPacker();
        var err = writer.Open(zipPath);
        if (err != Error.Ok)
            throw new InvalidOperationException($"Cannot open {zipPath} for writing.");

        writer.StartFile(ManifestPath);
        writer.WriteFile(Encoding.UTF8.GetBytes(manifest));
        writer.CloseFile();

        writer.StartFile(SqlitePath);
        writer.WriteFile(File.ReadAllBytes(sqlitePath));
        writer.CloseFile();
        writer.Close();
    }

    private static void ReadZip(string filePath, string sqlitePath)
    {
        var reader = new ZipReader();
        var err = reader.Open(filePath);
        if (err != Error.Ok)
            throw new InvalidOperationException($"Cannot open {filePath} as Ciallo project.");

        var manifestBytes = reader.ReadFile(ManifestPath);
        if (manifestBytes is not { Length: > 0 })
            throw new InvalidOperationException($"Missing {ManifestPath}.");

        var manifest = Encoding.UTF8.GetString(manifestBytes);
        if (!manifest.Contains($"\"formatVersion\": {FormatVersion},"))
            throw new InvalidOperationException("Unsupported Ciallo project format version.");

        var sqliteBytes = reader.ReadFile(SqlitePath);
        reader.Close();

        if (sqliteBytes is not { Length: > 0 })
            throw new InvalidOperationException($"Missing {SqlitePath}.");

        File.WriteAllBytes(sqlitePath, sqliteBytes);
    }

    #endregion

    #region Write

    private static void WriteSqlite(Entity document, string sqlitePath, ProjectFormatRegistry registry)
    {
        if (File.Exists(sqlitePath))
            File.Delete(sqlitePath);

        using var connection = new SqliteConnection($"Data Source={sqlitePath};Pooling=False");
        connection.Open();
        Execute(connection, "PRAGMA journal_mode=DELETE;");

        using var transaction = connection.BeginTransaction();
        Execute(connection, """
                            create table "metadata" (
                                "key" text primary key not null,
                                "value" text not null
                            );
                            """, transaction);
        Execute(connection, """
                            create table "entities" (
                                "id" integer primary key not null
                            );
                            """, transaction);
        InsertMetadata(connection, transaction, "format", "ciallo-project-sqlite");
        InsertMetadata(connection, transaction, "format_version", FormatVersion.ToString());
        InsertMetadata(connection, transaction, "created_by", "Ciallo");

        foreach (var component in registry.Components)
            CreateComponentTables(connection, transaction, component);

        var entities = BuildEntityList(document);
        var entityToId = new Dictionary<Entity, long>();
        for (int i = 0; i < entities.Count; i++)
            entityToId[entities[i]] = i;

        foreach (var (entity, id) in entities.Select((e, i) => (e, (long)i)))
        {
            InsertEntity(connection, transaction, id);
            foreach (var component in registry.Components)
            {
                if (!entity.ComponentTypes.Any(t => t.Type == component.ComponentType))
                    continue;

                var value = entity.Get(component.ComponentType);
                InsertComponent(connection, transaction, component, entity, id, value, entityToId);
            }
        }

        transaction.Commit();
    }

    private static List<Entity> BuildEntityList(Entity document)
    {
        var result = new List<Entity> { document };
        var query = document.World.CreateQuery().Tagged<ToSerializeTag>().Build();
        foreach (var entity in query.EnumerateWithEntities())
        {
            if (entity == document)
                continue;
            result.Add(entity);
        }
        return result;
    }

    private static string CreateChildTableSql(FieldDescriptor field)
    {
        return field.Shape switch
        {
            FieldShape.List => $"""
                                create table {Quote(field.ChildTableName)} (
                                    "owner_entity_id" integer not null,
                                    "item_index" integer not null,
                                    "ref_entity_id" integer not null,
                                    primary key ("owner_entity_id", "item_index")
                                );
                                """,
            FieldShape.Set => $"""
                               create table {Quote(field.ChildTableName)} (
                                   "owner_entity_id" integer not null,
                                   "ref_entity_id" integer not null,
                                   primary key ("owner_entity_id", "ref_entity_id")
                               );
                               """,
            FieldShape.IntKeyMap => $"""
                                     create table {Quote(field.ChildTableName)} (
                                         "owner_entity_id" integer not null,
                                         "key_int" integer not null,
                                         "ref_entity_id" integer not null,
                                         primary key ("owner_entity_id", "key_int")
                                     );
                                     """,
            _ => throw new ArgumentOutOfRangeException(nameof(field.Shape), field.Shape, null)
        };
    }

    private static IEnumerable<(string Column, object Value)> SerializeMainField(
        FieldDescriptor field,
        object value,
        Dictionary<Entity, long> entityToId)
    {
        switch (field.Shape)
        {
            case FieldShape.Scalar:
                yield return (field.Name, ScalarToDb(field, value));
                break;
            case FieldShape.Entity:
                yield return (field.EntityColumnName, EntityToDb(field, value, entityToId));
                break;
            case FieldShape.Blob:
                yield return (field.BlobColumnName, BlobToDb(field, value));
                break;
            case FieldShape.RawArray:
                var raw = RawArrayCodec.Encode(field, value);
                yield return (field.BlobColumnName, raw.Bytes);
                yield return (field.CountColumnName, raw.Count);
                break;
        }
    }

    private static object ScalarToDb(FieldDescriptor field, object value)
    {
        if (value == null)
            return DBNull.Value;

        var type = field.NonNullableValueType;
        if (type == typeof(bool))
            return (bool)value ? 1 : 0;
        if (type.IsEnum)
            return Convert.ToInt64(value);
        if (type == typeof(string))
            return value;
        if (type == typeof(float) || type == typeof(double))
            return Convert.ToDouble(value);
        return Convert.ToInt64(value);
    }

    private static object EntityToDb(
        FieldDescriptor field,
        object value,
        Dictionary<Entity, long> entityToId)
    {
        if (value == null)
            return DBNull.Value;

        var entity = (Entity)value;
        if (entity.IsNull)
        {
            if (field.EntityNullability == EntityNullability.Required)
                throw new InvalidOperationException($"Field {field.Component.Name}.{field.Name} is required but is Entity.Null.");
            return DBNull.Value;
        }
        if (!entityToId.TryGetValue(entity, out var id))
            throw new InvalidOperationException($"Field {field.Component.Name}.{field.Name} references an entity that is not persisted.");
        return id;
    }

    private static long ResolveEntityRef(
        Entity entity,
        FieldDescriptor field,
        Entity owner,
        Dictionary<Entity, long> entityToId)
    {
        if (entity.IsNull)
            throw new InvalidOperationException($"Collection field {field.Component.Name}.{field.Name} contains Entity.Null on {owner}.");

        if (!entityToId.TryGetValue(entity, out var id))
            throw new InvalidOperationException($"Collection field {field.Component.Name}.{field.Name} references an entity that is not persisted.");

        return id;
    }

    private static object BlobToDb(FieldDescriptor field, object value)
    {
        if (value == null)
            return DBNull.Value;
        return MessagePackSerializer.Serialize(field.ValueType, value);
    }

    #endregion

    #region Write SQL Commands

    private static void CreateComponentTables(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ComponentDescriptor component)
    {
        var columns = new List<string> { "\"entity_id\" integer primary key not null" };
        foreach (var field in component.Fields)
        {
            foreach (var column in field.MainColumns)
                columns.Add($"{Quote(column.Name)} {column.SqlType}");
        }

        Execute(connection,
            $"create table {Quote(component.TableName)} ({string.Join(", ", columns)});",
            transaction);

        foreach (var field in component.Fields.Where(f => f.IsChildTable))
            Execute(connection, CreateChildTableSql(field), transaction);
    }

    private static void InsertMetadata(SqliteConnection connection, SqliteTransaction transaction, string key, string value)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """insert into "metadata" ("key", "value") values ($key, $value);""";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }

    private static void InsertEntity(SqliteConnection connection, SqliteTransaction transaction, long id)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """insert into "entities" ("id") values ($id);""";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    private static void InsertComponent(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ComponentDescriptor descriptor,
        Entity owner,
        long ownerId,
        object component,
        Dictionary<Entity, long> entityToId)
    {
        var columns = new List<string> { "entity_id" };
        var parameters = new List<string> { "$entity_id" };
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.Parameters.AddWithValue("$entity_id", ownerId);

        foreach (var field in descriptor.Fields)
        {
            var value = field.GetProjectValue(component);
            if (field.IsChildTable)
            {
                columns.Add(field.ExistsColumnName);
                parameters.Add("$" + field.ExistsColumnName);
                command.Parameters.AddWithValue("$" + field.ExistsColumnName, value == null ? 0 : 1);
                if (value != null)
                    InsertChildRows(connection, transaction, field, owner, ownerId, value, entityToId);
                continue;
            }

            foreach (var (column, dbValue) in SerializeMainField(field, value, entityToId))
            {
                columns.Add(column);
                parameters.Add("$" + column);
                command.Parameters.AddWithValue("$" + column, dbValue ?? DBNull.Value);
            }
        }

        command.CommandText =
            $"insert into {Quote(descriptor.TableName)} ({string.Join(", ", columns.Select(Quote))}) values ({string.Join(", ", parameters)});";
        command.ExecuteNonQuery();
    }

    private static void InsertChildRows(
        SqliteConnection connection,
        SqliteTransaction transaction,
        FieldDescriptor field,
        Entity owner,
        long ownerId,
        object value,
        Dictionary<Entity, long> entityToId)
    {
        // V1 tradeoff: each child row currently creates and executes its own command.
        // Reuse prepared commands here if real project saves show this path is hot.
        switch (field.Shape)
        {
            case FieldShape.List:
                {
                    int index = 0;
                    foreach (var entity in EnumerateEntityCollection(field, value))
                        InsertListChild(connection, transaction, field, owner, ownerId, index++, ResolveEntityRef(entity, field, owner, entityToId));
                    break;
                }
            case FieldShape.Set:
                {
                    foreach (var entity in EnumerateEntityCollection(field, value))
                        InsertSetChild(connection, transaction, field, owner, ownerId, ResolveEntityRef(entity, field, owner, entityToId));
                    break;
                }
            case FieldShape.IntKeyMap:
                {
                    foreach (var (key, entity) in EnumerateEntityMap(field, value))
                        InsertMapChild(connection, transaction, field, owner, ownerId, key, ResolveEntityRef(entity, field, owner, entityToId));
                    break;
                }
        }
    }

    private static IEnumerable<Entity> EnumerateEntityCollection(FieldDescriptor field, object value)
    {
        if (value is IEnumerable<Entity> collection)
            return collection;

        throw new InvalidOperationException($"{field.Component.Name}.{field.Name} is not a supported entity collection.");
    }

    private static IEnumerable<KeyValuePair<int, Entity>> EnumerateEntityMap(FieldDescriptor field, object value)
    {
        if (value is ObservableSortedList<int, Entity> observableMap)
            return observableMap;

        if (value is IEnumerable<KeyValuePair<int, Entity>> map)
            return map;

        throw new InvalidOperationException($"{field.Component.Name}.{field.Name} is not a supported entity map.");
    }

    private static void InsertListChild(
        SqliteConnection connection,
        SqliteTransaction transaction,
        FieldDescriptor field,
        Entity owner,
        long ownerId,
        int index,
        long refId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"insert into {Quote(field.ChildTableName)} (\"owner_entity_id\", \"item_index\", \"ref_entity_id\") values ($owner, $idx, $ref);";
        command.Parameters.AddWithValue("$owner", ownerId);
        command.Parameters.AddWithValue("$idx", index);
        command.Parameters.AddWithValue("$ref", refId);
        command.ExecuteNonQuery();
    }

    private static void InsertSetChild(
        SqliteConnection connection,
        SqliteTransaction transaction,
        FieldDescriptor field,
        Entity owner,
        long ownerId,
        long refId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"insert into {Quote(field.ChildTableName)} (\"owner_entity_id\", \"ref_entity_id\") values ($owner, $ref);";
        command.Parameters.AddWithValue("$owner", ownerId);
        command.Parameters.AddWithValue("$ref", refId);
        command.ExecuteNonQuery();
    }

    private static void InsertMapChild(
        SqliteConnection connection,
        SqliteTransaction transaction,
        FieldDescriptor field,
        Entity owner,
        long ownerId,
        int key,
        long refId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"insert into {Quote(field.ChildTableName)} (\"owner_entity_id\", \"key_int\", \"ref_entity_id\") values ($owner, $key, $ref);";
        command.Parameters.AddWithValue("$owner", ownerId);
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$ref", refId);
        command.ExecuteNonQuery();
    }

    #endregion

    #region Read

    private static Entity ReadSqlite(string sqlitePath, ProjectFormatRegistry registry)
    {
        using var connection = new SqliteConnection($"Data Source={sqlitePath};Mode=ReadOnly;Pooling=False");
        connection.Open();

        var ids = ReadEntityIds(connection);
        if (!ids.Contains(0))
            throw new InvalidOperationException("Project database has no document entity id 0.");

        var world = new World();
        var idToEntity = new Dictionary<long, Entity>();
        foreach (var id in ids.OrderBy(i => i))
        {
            var entity = world.Create();
            entity.Tag<ToSerializeTag>();
            idToEntity[id] = entity;
        }

        // Deduplicate strings within a single load so identical values (e.g. layer
        // names repeated across thousands of rows) share one reference.
        var stringPool = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var component in registry.Components)
            ReadComponentTable(connection, component, idToEntity, stringPool);

        foreach (var component in registry.Components)
            ReadChildTables(connection, component, idToEntity);

        return idToEntity[0];
    }

    private static string ChildSelectSql(FieldDescriptor field)
    {
        return field.Shape switch
        {
            FieldShape.List =>
                $"select \"owner_entity_id\", \"item_index\", \"ref_entity_id\" from {Quote(field.ChildTableName)} order by \"owner_entity_id\", \"item_index\";",
            FieldShape.Set =>
                $"select \"owner_entity_id\", \"ref_entity_id\" from {Quote(field.ChildTableName)} order by \"owner_entity_id\", \"ref_entity_id\";",
            FieldShape.IntKeyMap =>
                $"select \"owner_entity_id\", \"key_int\", \"ref_entity_id\" from {Quote(field.ChildTableName)} order by \"owner_entity_id\", \"key_int\";",
            _ => throw new ArgumentOutOfRangeException(nameof(field.Shape), field.Shape, null)
        };
    }

    private static object DeserializeMainField(
        SqliteDataReader reader,
        FieldDescriptor field,
        Dictionary<long, Entity> idToEntity,
        Dictionary<string, string> stringPool)
    {
        switch (field.Shape)
        {
            case FieldShape.Scalar:
                return ScalarFromDb(reader, field, stringPool);
            case FieldShape.Entity:
                return EntityFromDb(reader, field, idToEntity);
            case FieldShape.Blob:
                return BlobFromDb(reader, field);
            case FieldShape.RawArray:
                return RawArrayFromDb(reader, field);
            default:
                throw new ArgumentOutOfRangeException(nameof(field.Shape), field.Shape, null);
        }
    }

    private static object ScalarFromDb(
        SqliteDataReader reader,
        FieldDescriptor field,
        Dictionary<string, string> stringPool)
    {
        var ordinal = reader.GetOrdinal(field.Name);
        if (reader.IsDBNull(ordinal))
        {
            if (field.IsNullable || field.NonNullableValueType == typeof(string))
                return null;
            throw new InvalidOperationException($"{field.Component.TableName}.{field.Name} is NULL.");
        }

        var type = field.NonNullableValueType;
        if (type == typeof(bool))
        {
            var v = reader.GetInt64(ordinal);
            return v switch
            {
                0 => false,
                1 => true,
                _ => throw new InvalidOperationException($"{field.Component.TableName}.{field.Name} has invalid bool value {v}.")
            };
        }
        if (type.IsEnum)
            return Enum.ToObject(type, reader.GetInt64(ordinal));
        if (type == typeof(string))
        {
            var s = reader.GetString(ordinal);
            if (s.Length == 0)
                return string.Empty;
            if (stringPool.TryGetValue(s, out var existing))
                return existing;
            stringPool[s] = s;
            return s;
        }
        if (type == typeof(float))
            return (float)reader.GetDouble(ordinal);
        if (type == typeof(double))
            return reader.GetDouble(ordinal);
        if (type == typeof(int))
            return (int)reader.GetInt64(ordinal);
        if (type == typeof(long))
            return reader.GetInt64(ordinal);
        if (type == typeof(short))
            return (short)reader.GetInt64(ordinal);
        if (type == typeof(byte))
            return (byte)reader.GetInt64(ordinal);

        return Convert.ChangeType(reader.GetInt64(ordinal), type);
    }

    private static object EntityFromDb(
        SqliteDataReader reader,
        FieldDescriptor field,
        Dictionary<long, Entity> idToEntity)
    {
        var ordinal = reader.GetOrdinal(field.EntityColumnName);
        if (reader.IsDBNull(ordinal))
            return Entity.Null;

        var id = reader.GetInt64(ordinal);
        if (!idToEntity.TryGetValue(id, out var entity))
            throw new InvalidOperationException($"{field.Component.Name}.{field.Name} references missing entity id {id}.");
        return entity;
    }

    private static object BlobFromDb(SqliteDataReader reader, FieldDescriptor field)
    {
        var ordinal = reader.GetOrdinal(field.BlobColumnName);
        if (reader.IsDBNull(ordinal))
        {
            if (field.IsNullable)
                return null;
            throw new InvalidOperationException($"{field.Component.TableName}.{field.BlobColumnName} is NULL.");
        }

        var bytes = (byte[])reader.GetValue(ordinal);
        return MessagePackSerializer.Deserialize(field.ValueType, bytes);
    }

    private static object RawArrayFromDb(SqliteDataReader reader, FieldDescriptor field)
    {
        var blobOrdinal = reader.GetOrdinal(field.BlobColumnName);
        var countOrdinal = reader.GetOrdinal(field.CountColumnName);
        if (reader.IsDBNull(blobOrdinal) || reader.IsDBNull(countOrdinal))
            throw new InvalidOperationException($"{field.Component.TableName}.{field.Name} raw array is NULL.");

        var bytes = (byte[])reader.GetValue(blobOrdinal);
        var count = checked((int)reader.GetInt64(countOrdinal));
        return RawArrayCodec.Decode(field, bytes, count);
    }

    private static Entity ResolveReadEntity(long id, FieldDescriptor field, Dictionary<long, Entity> idToEntity)
    {
        if (!idToEntity.TryGetValue(id, out var entity))
            throw new InvalidOperationException($"{field.Component.Name}.{field.Name} references missing entity id {id}.");
        return entity;
    }

    private static void AddEntitiesToCollection(FieldDescriptor field, object component, IReadOnlyList<Entity> entities)
    {
        var value = field.GetFieldStorageObject(component);
        if (value == null)
            throw new InvalidOperationException($"{field.Component.Name}.{field.Name} is null but has child rows.");

        switch (value)
        {
            case ObservableList<Entity> observableList:
                observableList.AddRange(entities);
                return;
            case ObservableHashSet<Entity> observableSet:
                observableSet.AddRange(entities);
                return;
            case ICollection<Entity> collection:
                foreach (var entity in entities)
                    collection.Add(entity);
                return;
            default:
                throw new InvalidOperationException($"{field.Component.Name}.{field.Name} is not a supported entity collection.");
        }
    }

    private static void AddEntityToMap(FieldDescriptor field, object component, int key, Entity entity)
    {
        var value = field.GetFieldStorageObject(component);
        if (value == null)
            throw new InvalidOperationException($"{field.Component.Name}.{field.Name} is null but has child rows.");

        switch (value)
        {
            case ObservableSortedList<int, Entity> observableMap:
                observableMap.Add(key, entity);
                return;
            case IDictionary<int, Entity> map:
                map.Add(key, entity);
                return;
            default:
                throw new InvalidOperationException($"{field.Component.Name}.{field.Name} is not a supported entity map.");
        }
    }

    #endregion

    #region Read SQL Commands

    private static List<long> ReadEntityIds(SqliteConnection connection)
    {
        var result = new List<long>();
        using var command = connection.CreateCommand();
        command.CommandText = """select "id" from "entities";""";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            result.Add(reader.GetInt64(0));
        return result;
    }

    private static void ReadComponentTable(
        SqliteConnection connection,
        ComponentDescriptor component,
        Dictionary<long, Entity> idToEntity,
        Dictionary<string, string> stringPool)
    {
        if (!TableExists(connection, component.TableName))
            return;

        var columns = GetColumns(connection, component.TableName);
        using var command = connection.CreateCommand();
        command.CommandText = $"select * from {Quote(component.TableName)};";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var entityId = reader.GetInt64(reader.GetOrdinal("entity_id"));
            if (!idToEntity.TryGetValue(entityId, out var entity))
                throw new InvalidOperationException($"{component.TableName}.entity_id {entityId} is not in entities.");

            var instance = Activator.CreateInstance(component.ComponentType)
                           ?? throw new InvalidOperationException($"Cannot create {component.ComponentType}.");

            foreach (var field in component.Fields.Where(f => !f.IsChildTable))
            {
                if (!field.MainColumns.All(c => columns.Contains(c.Name)))
                    continue;
                var value = DeserializeMainField(reader, field, idToEntity, stringPool);
                field.SetProjectValue(instance, value);
            }

            foreach (var field in component.Fields.Where(f => f.IsChildTable))
            {
                if (!columns.Contains(field.ExistsColumnName))
                    continue;
                var exists = ReadBool(reader, reader.GetOrdinal(field.ExistsColumnName), field);
                field.SetCollectionExists(instance, exists);
            }

            entity.AddAs(component.ComponentType, instance);
        }
    }

    private static void ReadChildTables(
        SqliteConnection connection,
        ComponentDescriptor component,
        Dictionary<long, Entity> idToEntity)
    {
        foreach (var field in component.Fields.Where(f => f.IsChildTable))
        {
            if (!TableExists(connection, field.ChildTableName))
                continue;

            if (field.Shape is FieldShape.List or FieldShape.Set)
            {
                ReadCollectionChildTable(connection, component, field, idToEntity);
                continue;
            }

            ReadMapChildTable(connection, component, field, idToEntity);
        }
    }

    private static void ReadCollectionChildTable(
        SqliteConnection connection,
        ComponentDescriptor component,
        FieldDescriptor field,
        Dictionary<long, Entity> idToEntity)
    {
        var batches = new Dictionary<long, List<Entity>>();

        using var command = connection.CreateCommand();
        command.CommandText = ChildSelectSql(field);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var ownerId = reader.GetInt64(0);
            ResolveChildOwner(field, component, idToEntity, ownerId);

            var refId = field.Shape == FieldShape.List ? reader.GetInt64(2) : reader.GetInt64(1);
            if (!batches.TryGetValue(ownerId, out var batch))
            {
                batch = [];
                batches.Add(ownerId, batch);
            }
            batch.Add(ResolveReadEntity(refId, field, idToEntity));
        }

        foreach (var (ownerId, entities) in batches)
        {
            var owner = idToEntity[ownerId];
            var componentInstance = owner.Get(component.ComponentType);
            AddEntitiesToCollection(field, componentInstance, entities);
        }
    }

    private static void ReadMapChildTable(
        SqliteConnection connection,
        ComponentDescriptor component,
        FieldDescriptor field,
        Dictionary<long, Entity> idToEntity)
    {
        using var command = connection.CreateCommand();
        command.CommandText = ChildSelectSql(field);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var ownerId = reader.GetInt64(0);
            var owner = ResolveChildOwner(field, component, idToEntity, ownerId);
            var componentInstance = owner.Get(component.ComponentType);
            AddEntityToMap(field, componentInstance, checked((int)reader.GetInt64(1)), ResolveReadEntity(reader.GetInt64(2), field, idToEntity));
        }
    }

    private static Entity ResolveChildOwner(
        FieldDescriptor field,
        ComponentDescriptor component,
        Dictionary<long, Entity> idToEntity,
        long ownerId)
    {
        if (!idToEntity.TryGetValue(ownerId, out var owner))
            throw new InvalidOperationException($"{field.ChildTableName}.owner_entity_id {ownerId} is not in entities.");
        if (!owner.ComponentTypes.Any(t => t.Type == component.ComponentType))
            throw new InvalidOperationException($"{field.ChildTableName} references entity {ownerId}, which has no {component.Name}.");

        return owner;
    }

    #endregion

    #region SQLite Utilities

    private static bool ReadBool(SqliteDataReader reader, int ordinal, FieldDescriptor field)
    {
        if (reader.IsDBNull(ordinal))
            return false;
        var value = reader.GetInt64(ordinal);
        return value switch
        {
            0 => false,
            1 => true,
            _ => throw new InvalidOperationException($"{field.Component.TableName}.{field.ExistsColumnName} has invalid bool value {value}.")
        };
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "select 1 from sqlite_master where type = 'table' and name = $name;";
        command.Parameters.AddWithValue("$name", tableName);
        return command.ExecuteScalar() != null;
    }

    private static HashSet<string> GetColumns(SqliteConnection connection, string tableName)
    {
        var result = new HashSet<string>();
        using var command = connection.CreateCommand();
        command.CommandText = $"pragma table_info({Quote(tableName)});";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            result.Add(reader.GetString(1));
        return result;
    }

    private static void Execute(SqliteConnection connection, string sql, SqliteTransaction transaction = null)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

    #endregion
}

internal sealed class ProjectFormatRegistry
{
    public IReadOnlyList<ComponentDescriptor> Components { get; }

    private ProjectFormatRegistry(IReadOnlyList<ComponentDescriptor> components)
    {
        Components = components;
    }

    public static ProjectFormatRegistry Create()
    {
        var components = AppDocumentManager.ToSerializeComponents
            .OrderBy(t => ComponentDescriptor.GetStorageName(t))
            .Select(ComponentDescriptor.Create)
            .ToArray();
        return new ProjectFormatRegistry(components);
    }
}

internal sealed class ComponentDescriptor
{
    public Type ComponentType { get; }
    public string Name { get; }
    public string TableName => "component_" + Name;
    public IReadOnlyList<FieldDescriptor> Fields { get; private set; }

    private ComponentDescriptor(Type componentType, string name, IReadOnlyList<FieldDescriptor> fields)
    {
        ComponentType = componentType;
        Name = name;
        Fields = fields;
    }

    public static ComponentDescriptor Create(Type type)
    {
        var name = GetStorageName(type);
        // Two-pass: build the final descriptor first, then populate its fields so
        // every FieldDescriptor.Component points to the real (non-dummy) instance.
        var descriptor = new ComponentDescriptor(type, name, []);
        var fields = EnumerateFields(type)
            .Select(field => FieldDescriptor.TryCreate(descriptor, field))
            .Where(field => field != null)
            .ToArray();
        descriptor.Fields = fields;
        return descriptor;
    }

    public static string GetStorageName(Type type)
    {
        var attr = type.GetCustomAttribute<ToSerializeAttribute>();
        return string.IsNullOrWhiteSpace(attr?.Name) ? type.Name : attr.Name;
    }

    private static IEnumerable<FieldInfo> EnumerateFields(Type type)
    {
        for (var cursor = type; cursor != null && cursor != typeof(object); cursor = cursor.BaseType)
        {
            foreach (var field in cursor.GetFields(
                         BindingFlags.Instance |
                         BindingFlags.Public |
                         BindingFlags.NonPublic |
                         BindingFlags.DeclaredOnly))
                yield return field;
        }
    }
}

internal readonly record struct ColumnDescriptor(string Name, string SqlType);

internal static class RawArrayCodec
{
    private static readonly MethodInfo EncodeMethod =
        typeof(RawArrayCodec).GetMethod(nameof(EncodeTyped), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo DecodeMethod =
        typeof(RawArrayCodec).GetMethod(nameof(DecodeTyped), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ImmutableArrayMethod =
        typeof(RawArrayCodec).GetMethod(nameof(ToImmutableArray), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static bool IsSupportedElementType(Type type)
    {
        return type == typeof(float)
               || type == typeof(int)
               || type == typeof(Vector2);
    }

    public static (byte[] Bytes, int Count) Encode(FieldDescriptor field, object value)
    {
        if (value == null)
            return (null, 0);

        var array = ToArray(value, field.ElementType);
        var method = EncodeMethod.MakeGenericMethod(field.ElementType);
        return ((byte[] Bytes, int Count))method.Invoke(null, [array]);
    }

    public static object Decode(FieldDescriptor field, byte[] bytes, int count)
    {
        var method = DecodeMethod.MakeGenericMethod(field.ElementType);
        var array = method.Invoke(null, [bytes, count]);
        return FromArray(array, field.NonNullableValueType, field.ElementType);
    }

    private static (byte[] Bytes, int Count) EncodeTyped<T>(T[] values) where T : struct
    {
        var bytes = MemoryMarshal.AsBytes(values.AsSpan()).ToArray();
        return (bytes, values.Length);
    }

    private static T[] DecodeTyped<T>(byte[] bytes, int count) where T : struct
    {
        var size = Marshal.SizeOf<T>();
        if (bytes.Length != checked(count * size))
            throw new InvalidOperationException($"Raw array byte length {bytes.Length} does not match count {count} and element size {size}.");

        var result = new T[count];
        bytes.CopyTo(MemoryMarshal.AsBytes(result.AsSpan()));
        return result;
    }

    private static object ToArray(object value, Type elementType)
    {
        var valueType = value.GetType();
        if (valueType.IsArray)
            return value;

        if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
        {
            var isDefault = (bool)valueType.GetProperty("IsDefault")!.GetValue(value)!;
            if (isDefault)
                return Array.CreateInstance(elementType, 0);
        }

        // V1 tradeoff: this generic path boxes struct elements such as Vector2.
        // Add typed ImmutableArray<T>/List<T> paths later if raw array encoding becomes hot.
        var values = ((IEnumerable)value).Cast<object>().ToArray();
        var array = Array.CreateInstance(elementType, values.Length);
        for (int i = 0; i < values.Length; i++)
            array.SetValue(values[i], i);
        return array;
    }

    private static object FromArray(object array, Type containerType, Type elementType)
    {
        if (containerType.IsArray)
            return array;
        if (containerType.IsGenericType && containerType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
            foreach (var item in (IEnumerable)array)
                list.Add(item);
            return list;
        }
        if (containerType.IsGenericType && containerType.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
        {
            return ImmutableArrayMethod.MakeGenericMethod(elementType).Invoke(null, [array]);
        }

        throw new InvalidOperationException($"{containerType} is not a supported raw array container.");
    }

    private static ImmutableArray<T> ToImmutableArray<T>(T[] values)
    {
        return ImmutableCollectionsMarshal.AsImmutableArray(values);
    }
}
