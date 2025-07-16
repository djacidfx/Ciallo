using Godot;
using Newtonsoft.Json;

namespace Ciallo.Core;

public class Preferences
{
    
    #region save load json
    public static readonly string Path = "res://Temp/Config.json";
    [JsonIgnore]
    public bool FileExists = false;

    public Preferences(bool fromFile = false)
    {
        if (!fromFile) return;
        if (!FileAccess.FileExists(Path))
        {
            return;
        }
        
        FileExists = true;
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
        string content = file.GetAsText();
        JsonConvert.PopulateObject(content, this);
    }

    public void Save()
    {
        var content = JsonConvert.SerializeObject(this);
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        file.StoreString(content);
    }
    #endregion
}