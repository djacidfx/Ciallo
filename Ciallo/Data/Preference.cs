using System.Collections.Generic;
using System.Runtime.Serialization;
using Ciallo.Misc;
using Godot;
using Newtonsoft.Json;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

[DataContract]
public class Preference
{
    #region WorldView
    public ReactiveProperty<Viewport.Msaa> Msaa = new(Viewport.Msaa.Msaa4X);
    public ReactiveProperty<bool> UseTAA = new(false);
    public ReactiveProperty<bool> UseFXAA = new(false);
    public readonly ReactiveProperty<float> MouseWheelZoomFactor = new(0.1f);

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

    [DataMember]
    public ReactiveProperty<string> Language = new("en");
    [DataMember]
    public ObservableList<string> RecentFiles = [];
    
    [DataMember]
    public Color StrokeWireframeColor = Colors.NavyBlue;
    [DataMember]
    public Color StrokeWireframeHintColor = Colors.Orange;
    
    #region Save Load Json
    public static readonly string Path = "user://Preference.json";

    public static readonly JsonSerializerSettings JsonOptions = new()
    {
        Converters =
        {
            ReactivePropertyConverter.Instance,
        }
    };

    public bool TryLoad()
    {
        if (!FileAccess.FileExists(Path))
            return false;
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
        string content = file.GetAsText();
        
        JsonConvert.PopulateObject(content,this, JsonOptions);
        return true;
    }

    public void Save()
    {
        var content = JsonConvert.SerializeObject(this, JsonOptions);
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        file.StoreString(content);
    }
    #endregion
}