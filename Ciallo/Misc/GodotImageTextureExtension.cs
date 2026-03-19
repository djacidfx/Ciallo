using Godot;

namespace Ciallo;

public static class GodotImageTextureExtension
{
    public static readonly ImageTexture DummyTexture = ImageTexture.CreateFromImage(GodotImageExtension.DummyImage);

    extension(ImageTexture texture)
    {
        public bool IsInitialized => texture.GetWidth() != 0;
        public static ImageTexture Dummy => DummyTexture;
    }
}

public static class GodotImageExtension
{
    public static readonly Image DummyImage = Image.CreateFromData(1, 1, true, Image.Format.L8, new byte[] { 255 });

    extension(Image)
    {
        public static Image Dummy => DummyImage;
    }
}