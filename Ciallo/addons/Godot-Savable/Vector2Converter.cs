using System;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Saveable.Converters;

public class Vector2Converter : JsonConverter<Vector2>
{
    public static Vector2Converter Instance { get; } = new Vector2Converter();
    
    public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var obj = JObject.Load(reader);
        float x = obj.Value<float>("x");
        float y = obj.Value<float>("y");
        return new Vector2(x, y);
    }

    public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("x");
        writer.WriteValue(value.X);
        writer.WritePropertyName("y");
        writer.WriteValue(value.Y);
        writer.WriteEndObject();
    }
}