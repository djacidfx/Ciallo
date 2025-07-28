using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ciallo.Misc;
using Godot;
using R3;

namespace Ciallo.Core;

public class ProgramPreference
{
    #region world2D
    public ReactiveProperty<Viewport.Msaa> Msaa = new(Viewport.Msaa.Msaa4X);
    public ReactiveProperty<bool> UseTAA = new(false);
    public ReactiveProperty<bool> UseFXAA = new(false);
    #endregion
    
    public ReactiveProperty<string> Language = new("en");
    public List<string> RecentFiles = [];
    
    #region save load json
    public static readonly string Path = "res://Temp/Preferences.json";
    public static bool FileExists = false;
    private static JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        Converters = { new ReactivePropertyJsonConverterFactory(), new JsonStringEnumConverter() }
    };

    public static ProgramPreference Load()
    {
        if (!FileAccess.FileExists(Path))
        {
            FileExists = false;
            return new();
        }
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
        string content = file.GetAsText();
        return JsonSerializer.Deserialize<ProgramPreference>(content, _options);
    }

    public void Save()
    {
        var content = JsonSerializer.Serialize(this, _options);
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        file.StoreString(content);
    }
    #endregion
}