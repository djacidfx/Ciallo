using Newtonsoft.Json;
using System;
using System.Reflection;

public class ReactivePropertyConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsGenericType &&
               objectType.GetGenericTypeDefinition().Name == "ReactiveProperty`1";
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if(value == null) 
        {
            writer.WriteNull();
            return;
        }
        var valueProperty = value.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
        var innerValue = valueProperty!.GetValue(value);
        serializer.Serialize(writer, innerValue);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }
        
        var valueType = objectType.GetGenericArguments()[0];
        var deserializedValue = serializer.Deserialize(reader, valueType);
        if (existingValue == null) return Activator.CreateInstance(objectType, deserializedValue);
        // set Value to deserializedValue
        var valueProperty = existingValue.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
        valueProperty!.SetValue(existingValue, deserializedValue);
        return existingValue;
    }
}