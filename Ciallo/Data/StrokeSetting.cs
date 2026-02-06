using System.Runtime.Serialization;
using Frent;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class StrokeSetting
{
    [DataMember] public Entity BrushE = Entity.Null;
    [DataMember] public ReactiveProperty<Color> OverrideColor = null;
}