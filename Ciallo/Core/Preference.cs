using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using R3;

namespace Ciallo.Core;

public partial class Preference : Node
{
    #region WorldView
    public ReactiveProperty<Viewport.Msaa> Msaa = new(Viewport.Msaa.Msaa4X);
    public ReactiveProperty<bool> UseTAA = new(false);
    public ReactiveProperty<bool> UseFXAA = new(false);
    public ReactiveProperty<float> MouseWheelZoomFactor = new(0.1f);
    #endregion
    
    public ReactiveProperty<string> Language = new("en");
    public List<string> RecentFiles = [];
    
    #region save load json
    public class IntPtrIgnorer : JsonConverter<IntPtr>
    {
        public override IntPtr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => IntPtr.Zero;

        public override void Write(Utf8JsonWriter writer, IntPtr value, JsonSerializerOptions options)
            => writer.WriteNullValue(); // or writer.WriteStringValue(""); etc.
    }

    
    public static readonly string Path = "res://Temp/Preferences.json";
    public static bool FileExists = false;
    private static JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        IncludeFields = false,
        Converters = { new ReactivePropertyJsonConverterFactory(), new JsonStringEnumConverter(), new IntPtrIgnorer()}
    };

    public static Preference Load()
    {
        if (!FileAccess.FileExists(Path))
        {
            FileExists = false;
            return new();
        }
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
        string content = file.GetAsText();
        return JsonSerializer.Deserialize<Preference>(content, _options);
    }

    public void Save()
    {
        var content = JsonSerializer.Serialize(this, _options);
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        file.StoreString(content);
    }
    #endregion
}