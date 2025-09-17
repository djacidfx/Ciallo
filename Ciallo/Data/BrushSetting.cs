using System.Runtime.Serialization;
using Ciallo.Geometry;
using Ciallo.Tool;
using Ciallo.Widget;
using Godot;
using R3;
using MessagePack;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class BrushSetting : IPropertySource
{
    [DataMember] public ReactiveProperty<BrushType> Type = new(BrushType.Stamp);
    
    [DataMember] public ReactiveProperty<Color> Color = new(Colors.Black); // RGB+Flow
    [DataMember] public BezierCurve Pressure2RadiusRatioCurve = BezierCurve.Linear(); // radius = baseRadius * curve(pressure)

    // Vanilla
    [DataMember] public ReactiveProperty<float> DashLength = new(-1.0f);
    [DataMember] public ReactiveProperty<float> GapLength = new(-1.0f);
    [DataMember] public ReactiveProperty<float> DashForwardSpeed = new(0.0f);
    
    // Stamp
    [DataMember] public ReactiveProperty<float> StampInterval = new(0.4f); // in radius unit
    
    // Airbrush
    [DataMember] public BezierCurve FalloffCurve = BezierCurve.Linear(1.0f, 0.0f);
    
    public void DrawProperty(PropertyContainer container)
    {
        
    }
}

public enum BrushType
{
    Vanilla = 0,
    Stamp,
    Airbrush,
}