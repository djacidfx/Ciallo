using System;
using MessagePack;
using MessagePack.Formatters;
using R3;

namespace Ciallo;

public class ReactivePropertyFormatter<T> : IMessagePackFormatter<ReactiveProperty<T>>
{
    public void Serialize(ref MessagePackWriter writer, ReactiveProperty<T> property, MessagePackSerializerOptions options)
    {
        if (property == null)
        {
            // By design of Ciallo, reactive property itself cannot be null
            throw new ArgumentNullException(nameof(property), "ReactiveProperty cannot be null.");
        }
        var formatter = options.Resolver.GetFormatterWithVerify<T>();
        formatter.Serialize(ref writer, property.Value, options);
    }

    public ReactiveProperty<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil()) // inner value is null
        {
            return new ReactiveProperty<T>(default);
        }
        var formatter = options.Resolver.GetFormatterWithVerify<T>();
        var inner = formatter.Deserialize(ref reader, options);
        return new ReactiveProperty<T>(inner);
    }
}

public class ReactivePropertyResolver : IFormatterResolver
{
    public static readonly IFormatterResolver Instance = new ReactivePropertyResolver();
    private ReactivePropertyResolver() { }

    public IMessagePackFormatter<T> GetFormatter<T>()
    {
        var t = typeof(T);
        if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ReactiveProperty<>))
        {
            // make ReactivePropertyFormatter<Inner>
            var inner = t.GetGenericArguments()[0];
            var formatterType = typeof(ReactivePropertyFormatter<>).MakeGenericType(inner);
            return (IMessagePackFormatter<T>)Activator.CreateInstance(formatterType);
        }

        // fall back to next resolver
        return null;
    }
}