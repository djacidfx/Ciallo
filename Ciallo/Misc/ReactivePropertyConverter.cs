using System;
using Newtonsoft.Json;
using R3;

namespace Ciallo.Misc;

public class ReactivePropertyConverter<T> : JsonConverter<ReactiveProperty<T>>
{
    public override void WriteJson(JsonWriter writer, ReactiveProperty<T> value, JsonSerializer serializer)
    {
        writer.WriteValue(value.CurrentValue);
    }

    public override ReactiveProperty<T> ReadJson(JsonReader reader, Type objectType, ReactiveProperty<T> existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        T x = (T)reader.Value;
        if (hasExistingValue)
        {
            existingValue.Value = x;
            return existingValue;
        };
        return new ReactiveProperty<T>(x);
    }
}