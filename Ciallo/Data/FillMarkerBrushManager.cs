using System.Runtime.Serialization;
using Frent;
using ObservableCollections;

namespace Ciallo.Data;

// Not implement related editing Gui
[DataContract]
public class FillMarkerBrushManager
{
    [DataMember] public ObservableList<Entity> BrushEs = [];
}