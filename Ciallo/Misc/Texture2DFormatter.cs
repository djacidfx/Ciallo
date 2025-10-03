using Godot;
using MessagePack;
using MessagePack.Formatters;

namespace Ciallo.Misc;

public class Texture2DFormatter : IMessagePackFormatter<Texture2D>
{
    public static readonly Texture2DFormatter Instance = new();
    
    public void Serialize(ref MessagePackWriter writer, Texture2D value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }
        var image = value.GetImage();
        MessagePackSerializer.Serialize(ref writer, image, options);
    }

    public Texture2D Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }
        var image = MessagePackSerializer.Deserialize<Image>(ref reader, options);
        var texture = ImageTexture.CreateFromImage(image);
        return texture;
    }
}

public class ImageFormatter : IMessagePackFormatter<Image>
{
    public static readonly ImageFormatter Instance = new();
    
    public void Serialize(ref MessagePackWriter writer, Image value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }
        // Serialize as [width, height, format, data]
        writer.WriteArrayHeader(4);
        writer.Write(value.GetWidth());
        writer.Write(value.GetHeight());
        writer.Write((int)value.GetFormat());
        // Raw pixel data
        var data = value.GetData();
        writer.Write(data);
    }

    public Image Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }
        // Expect [width, height, format, data]
        _ = reader.ReadArrayHeader();
        // Let it crash if count != 4
        var width = reader.ReadInt32();
        var height = reader.ReadInt32();
        var format = (Image.Format)reader.ReadInt32();
        var data = MessagePackSerializer.Deserialize<byte[]>(ref reader, options);

        var image = Image.CreateFromData(width, height, false, format, data);
        image.GenerateMipmaps();
        return image;
    }
}