using System.Runtime.Serialization;
using Frent;
using ObservableCollections;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class BrushManager
{
    [DataMember] public ObservableList<Entity> Brushes = [];

    public int Add(Entity brush)
    {
        Brushes.Add(brush);
        return Brushes.Count - 1;
    }

    public void Remove(Entity brush)
    {
        Brushes.Remove(brush);
    }
}