using System.Runtime.Serialization;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class FilledPolygonSetting
{
    // Same as Polygon2D fill color. If texture is set, it will be multiplied by this color.
    [DataMember] public ReactiveProperty<Color> Color = new(Colors.White);
}