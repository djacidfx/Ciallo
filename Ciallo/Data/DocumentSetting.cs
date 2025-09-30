using System.Runtime.Serialization;
using R3;
using Godot;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class DocumentSetting
{
    [DataMember] public ReactiveProperty<string> Name = new();
    // Reference size is used for background size, default export size, import image size, etc.
    [DataMember] public ReactiveProperty<Vector2> ReferenceSize = new(new(1920, 1080));
    [DataMember] public ReactiveProperty<Color> BackgroundColor = new(Colors.White);
    
    public string FilePath = new(OS.GetSystemDir(OS.SystemDir.Documents));
}