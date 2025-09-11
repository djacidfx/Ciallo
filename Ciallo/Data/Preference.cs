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

    public List<string> SupportedLanguages = 
    [
        "en",
        "fr",
        "de",
        "ja",
        "ko",
        "zh_CN",
        "zh_TW",
    ];

    #endregion

    [JsonProperty]
    public ReactiveProperty<string> Language = new("en");
    [JsonProperty]
    public List<string> RecentFiles = [];
    
    [JsonProperty]
    public Color StrokeWireframeColor = Colors.NavyBlue;
    
    #region Save Load Json
    public static readonly string Path = "res://Temp/Preference.json";
    private static JsonSerializerSettings _options = new()
    {
        Converters = {new ReactivePropertyConverter()}
    };

    public bool TryLoad()
    {
        if (!FileAccess.FileExists(Path))
            return false;
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
        string content = file.GetAsText();
        
        JsonConvert.PopulateObject(content,this, _options);
        return true;
    }

    public void Save()
    {
        var content = JsonConvert.SerializeObject(this, _options);
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        file.StoreString(content);
    }
    #endregion
}