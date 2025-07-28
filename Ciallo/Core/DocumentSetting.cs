using R3;
using Godot;

namespace Ciallo.Core;

public class DocumentSetting
{
    // Reference size is used for background size, default export size, import image size, etc.
    public ReactiveProperty<Vector2I> ReferenceSize = new(new Vector2I(1920, 1080));
}