using System.IO;
using Godot;

namespace Ciallo.GuiControl;

internal static class ExportPngWriter
{
    public static void SaveHdr2DViewportAsPng(SubViewport viewport, string path)
    {
        var image = viewport.GetTexture().GetImage();

        // Export viewports render through the same HDR 2D path as the paint viewport
        // so blended edges and translucent colors match what the user saw. The HDR
        // readback is a linear float format (RGBAH); LinearToSrgb only accepts 8-bit
        // RGB8/RGBA8, so quantize to Rgba8 first, then apply the sRGB delivery curve.
        image.Convert(Image.Format.Rgba8);
        image.LinearToSrgb();

        var error = image.SavePng(path);
        if (error != Error.Ok)
            throw new IOException($"Failed to save PNG to '{path}': {error}");
    }
}
