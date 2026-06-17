using System.IO;
using Godot;

namespace Ciallo.GuiControl;

internal static class ExportPngWriter
{
    public static void SaveHdr2DViewportAsPng(SubViewport viewport, string path)
    {
        var image = viewport.GetTexture().GetImage();

        // Export viewports render through the same HDR 2D path as the paint viewport
        // so blended edges and translucent colors match what the user saw. PNG is an
        // sRGB delivery format here, so the linear HDR readback must be encoded first.
        image.LinearToSrgb();
        image.Convert(Image.Format.Rgba8);

        var error = image.SavePng(path);
        if (error != Error.Ok)
            throw new IOException($"Failed to save PNG to '{path}': {error}");
    }
}
