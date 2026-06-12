using System;
using System.Text.RegularExpressions;
using MessagePack;
using MessagePack.Formatters;

namespace Ciallo;

public class TypeFormatter : IMessagePackFormatter<Type>
{
    public static readonly TypeFormatter Instance = new();
    // Copied from the MemoryPack package, src/MemoryPack.Core/Formatters/TypeFormatter.cs
    private static readonly Regex ShortTypeRegex = new(@", Version=\d+.\d+.\d+.\d+, Culture=[\w-]+, PublicKeyToken=(?:null|[a-f0-9]{16})", RegexOptions.Compiled);

    public void Serialize(ref MessagePackWriter writer, Type value, MessagePackSerializerOptions options)
    {
        if (value?.AssemblyQualifiedName == null)
        {
            writer.WriteNil();
            return;
        }

        writer.Write(ShortTypeRegex.Replace(value.AssemblyQualifiedName, ""));
    }

    public Type Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }
        var assemblyQualifiedName = reader.ReadString() ?? throw new InvalidOperationException("Assembly qualified name is null.");
        return Type.GetType(assemblyQualifiedName, throwOnError: false);
    }
}