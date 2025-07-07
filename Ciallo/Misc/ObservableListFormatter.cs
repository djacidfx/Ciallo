using System.Collections.Generic;
using System.Linq;
using MemoryPack;
using ObservableCollections;

namespace Ciallo;

public class ObservableListFormatter<T> : MemoryPackFormatter<ObservableList<T>>
{
    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ObservableList<T> value)
    {
        if (value == null)
        {
            writer.WriteNullObjectHeader();
            return;
        }
        writer.WriteValue(value.ToList());
    }

    public override void Deserialize(ref MemoryPackReader reader, scoped ref ObservableList<T> value)
    {
        if (reader.PeekIsNull())
        {
            reader.Advance(1); // skip null block
            value = null;
            return;
        }
        var v = reader.ReadValue<List<T>>();
        value = new(v);
    }
}
