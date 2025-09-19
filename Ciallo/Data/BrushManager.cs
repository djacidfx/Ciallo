using System.Collections.Generic;
using System.Runtime.Serialization;
using Arch.Core;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class BrushManager
{
    public List<Entity> Brushes;
}