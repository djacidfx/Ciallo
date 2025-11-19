using System.Runtime.Serialization;
using Frent;
using Godot;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class StrokeSetting
{
    [DataMember] public Entity BrushE = Entity.Null;
    [DataMember] public bool OverrideColor = false;
    [DataMember] public Color Color = Colors.White;
}