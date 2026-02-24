using System.Collections.Generic;
using System.Runtime.Serialization;
using Ciallo.Geometry;
using Godot;
using Newtonsoft.Json;
using ObservableCollections;
using R3;
using FileAccess = Godot.FileAccess;

namespace Ciallo.Data;

[DataContract]
public class Preference
{
    public readonly ReactiveProperty<float> MouseWheelZoomFactor = new(0.1f);
    public readonly ReactiveProperty<float> MouseWheelRotateFactor = new(Mathf.Pi / 36);

    public static readonly List<string> SupportedLanguages =
    [
        "en",
        "fr",
        "de",
        "ja",
        "ko",
        "zh_CN",
        "zh_TW",
    ];

    [DataMember]
    public Window.ModeEnum WindowMode;
    [DataMember]
    public Vector2I WindowPosition = new(0, 0);
    [DataMember]
    public Vector2I WindowSize = new(1920, 1080);
    [DataMember]
    public ReactiveProperty<string> Language = new("en");
    [DataMember]
    public ObservableList<string> RecentFiles = [];

    [DataMember]
    public Color StrokeWireframeColor = Colors.Orange;
    [DataMember]
    public float StrokeWireframeRadius = 2f;
    [DataMember]
    public float StrokeDotRadius = 12f;

    [DataMember]
    public BezierCurve PenPressureRemapCurve = BezierCurve.EaseInOut();

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

        JsonConvert.PopulateObject(content, this, JsonOptions);
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