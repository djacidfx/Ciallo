using System;
using Godot;
using R3;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Ciallo.Data;

[JsonObject(MemberSerialization.OptIn)]
public class Preference
{
    #region WorldView
    public ReactiveProperty<Viewport.Msaa> Msaa = new(Viewport.Msaa.Msaa4X);
    public ReactiveProperty<bool> UseTAA = new(false);
    public ReactiveProperty<bool> UseFXAA = new(false);
    public ReactiveProperty<float> MouseWheelZoomFactor = new(0.1f);
    #endregion
    
    [JsonProperty]
    public ReactiveProperty<string> Language = new("en");
    [JsonProperty]
    public List<string> RecentFiles = [];
    
    #region save load json
    public static readonly string Path = "res://Temp/Preference.json";
    public static bool FileExists = false;
    private static JsonSerializerSettings _options = new()
    {
        Converters = {new ReactivePropertyConverter()}
    };

    public void Load()
    {
        if (!FileAccess.FileExists(Path))
        {
            FileExists = false;
            return;
        }
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
        string content = file.GetAsText();
        
        JsonConvert.PopulateObject(content,this, _options);
    }

    public void Save()
    {
        var content = JsonConvert.SerializeObject(this, _options);
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        file.StoreString(content);
    }
    #endregion
}