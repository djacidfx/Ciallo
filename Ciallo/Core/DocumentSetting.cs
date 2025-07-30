using R3;
using Godot;

namespace Ciallo.Core;

public class DocumentSetting
{
    // Reference size is used for background size, default export size, import image size, etc.
    public ReactiveProperty<Vector2> ReferenceSize = new(new Vector2(1920, 1080));
    public ReactiveProperty<Color> BackgroundColor = new(Colors.White);
    public string FilePath = new(OS.GetSystemDir(OS.SystemDir.Documents));
}