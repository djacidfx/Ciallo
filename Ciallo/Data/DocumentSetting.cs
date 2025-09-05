using R3;
using Godot;
using MessagePack;

namespace Ciallo.Data;

[MessagePackObject(true), ToSerialize]
public class DocumentSetting
{
    public readonly ReactiveProperty<string> Name = new();
    // Reference size is used for background size, default export size, import image size, etc.
    public readonly ReactiveProperty<Vector2> ReferenceSize = new(new(1920, 1080));
    public readonly ReactiveProperty<Color> BackgroundColor = new(Colors.White);
    [IgnoreMember]
    public string FilePath = new(OS.GetSystemDir(OS.SystemDir.Documents));
}