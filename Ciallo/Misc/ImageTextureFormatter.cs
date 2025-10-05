using Godot;
using MessagePack;
using MessagePack.Formatters;

namespace Ciallo.Misc;

public class ImageTextureFormatter : IMessagePackFormatter<ImageTexture>
{
    public static readonly ImageTextureFormatter Instance = new();
    
    public void Serialize(ref MessagePackWriter writer, ImageTexture value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
            return;
        }
        var image = value.GetImage();
        MessagePackSerializer.Serialize(ref writer, image, options);
    }

    public ImageTexture Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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
        
        writer.Write(value.SavePngToBuffer());
    }

    public Image Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return null;
        }
        // Image class doesn't have a static function like Image.CreateLoadFromBuffer
        var data = MessagePackSerializer.Deserialize<byte[]>(ref reader, options);
        var image = Image.CreateEmpty(1, 1, false, Image.Format.L8);
        image.LoadPngFromBuffer(data);
        image.GenerateMipmaps();
        return image;
    }
}