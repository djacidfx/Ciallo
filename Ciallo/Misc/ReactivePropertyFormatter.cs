using R3;
using MemoryPack;

namespace Ciallo.Misc;

/// <summary>
/// ReactiveProperty format to binary with MemoryPack.
/// </summary>
public class ReactivePropertyFormatter<T> : MemoryPackFormatter<ReactiveProperty<T>>
{
    public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ReactiveProperty<T> property)
    {
        if (property == null)
        {
            writer.WriteNullObjectHeader();
            return;
        }
        writer.WriteValue(property.Value);
    }

    public override void Deserialize(ref MemoryPackReader reader, scoped ref ReactiveProperty<T> property)
    {
        if (reader.PeekIsNull())
        {
            reader.Advance(1); // skip null block
            property = null;
            return;
        }
        var value = reader.ReadValue<T>();
        if (property == null)
        {
            property = new(value);
        }
        else
        {
            property.Value = value;
        }
    }
}
