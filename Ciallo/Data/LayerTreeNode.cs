using System.Runtime.Serialization;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class LayerTreeNode : EntityTreeNode<LayerTreeNode>
{
    [DataMember] public ReactiveProperty<string> Name = new("");
    [DataMember] public ReactiveProperty<bool> IsVisible = new(true);
    [DataMember] public ReactiveProperty<float> Opacity = new(1.0f);
    [DataMember] public ReactiveProperty<bool> IsLocked = new(false); // Need to implement
}