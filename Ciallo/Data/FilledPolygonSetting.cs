using System.Runtime.Serialization;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class FilledPolygonSetting
{
    // Same as Polygon2D fill color. If texture is set, it will be multiplied by this color.
    public ReactiveProperty<Color> Color { get; } = new(Colors.White);
}