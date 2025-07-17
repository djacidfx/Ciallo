using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using R3;

namespace Ciallo.Misc;

public class ReactivePropertyConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
        {
            return false;
        }

        if (typeToConvert.GetGenericTypeDefinition() != typeof(ReactiveProperty<>))
        {
            return false;
        }

        return true;
    }
    public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
    {
        Type[] typeArguments = type.GetGenericArguments();
        Type valueType = typeArguments[0]; ;

        JsonConverter converter = (JsonConverter)Activator.CreateInstance(
            typeof(ReactivePropertyConverter<>).MakeGenericType([valueType]),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            args: [options],
            culture: null)!;

        return converter;
    }
}


public class ReactivePropertyConverter<T> : JsonConverter<ReactiveProperty<T>>
{
    private readonly JsonConverter<T> _valueConverter;
    
    public ReactivePropertyConverter(JsonSerializerOptions options)
    {
        _valueConverter = (JsonConverter<T>)options.GetConverter(typeof(T));
    }

    public override ReactiveProperty<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var property = new ReactiveProperty<T>(default);
        var value = _valueConverter.Read(ref reader, typeToConvert, options);
        property.Value = value;
        return property;
    }

    public override void Write(Utf8JsonWriter writer, ReactiveProperty<T> property, JsonSerializerOptions options)
    {
        _valueConverter.Write(writer, property.Value, options);
    }
}