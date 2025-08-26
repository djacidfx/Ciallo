using MessagePack;

namespace Ciallo.Data;

/// <summary>
/// User defined name for both branch and leaf nodes.
/// </summary>
[MessagePackObject, ToSerialize]
public class LayerTreeName
{
    [Key(0)]
    public string Name;

    public LayerTreeName(string name) => Name = name;
    public static implicit operator string(LayerTreeName x) => x.Name;
    public static implicit operator LayerTreeName(string s) => new LayerTreeName(s);
    public override string ToString() => Name;
}