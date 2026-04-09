using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Command;

[CommandBuilder]
public class NewStrokeBrushCmd : CommandBase
{
    private StrokeBrushSetting _setting;
    public readonly Entity CopyE;

    public NewStrokeBrushCmd(StrokeBrushSetting setting = null)
    {
        _setting = setting;
    }

    public NewStrokeBrushCmd(Entity copyE = default)
    {
        CopyE = copyE;
    }

    public override void OnDeletedAsDo() => TargetE.Delete();

    public override void BeforeFirstDo(Entity targetE)
    {
        _setting ??= CopyE.IsNull
            ? new StrokeBrushSetting()
            : CopyE.Get<StrokeBrushSetting>().Clone();
        targetE.Add(_setting);

        // material
        var material = new StrokeBrushMaterial();
        material.ObserveBrushSetting(_setting);
        targetE.Add(material);

        // preview texture
        Vector2I size = new(256 * 2, 256);
        var viewport = new SubViewport()
        {
            RenderTargetUpdateMode = SubViewport.UpdateMode.WhenVisible,
            RenderTargetClearMode = SubViewport.ClearMode.Always,
            Size = size,
            UseHdr2D = true,
            Disable3D = true,
            TransparentBg = true,
        }.QueueFreeWith(targetE);

        var previewStroke = new StrokeView()
        {
            Material = material,
        };
        previewStroke.SetGeometry(CreatePreviewGeometry(), 12f);
        viewport.AddChild(previewStroke);

        Document.Get<SubViewportHolder>().AddChild(viewport);
        targetE.Add(viewport.GetTexture());
    }

    public override void Do(Entity targetE)
    {
        // Data
        targetE.Tag<ToSerializeTag>();
        var bm = Document.Get<BrushManager>();
        bm.StrokeBrushEs.Add(targetE);
    }

    public override void Undo(Entity brushE)
    {
        var bm = Document.Get<BrushManager>();
        bm.StrokeBrushEs.Remove(brushE);
        brushE.Detach<ToSerializeTag>();
    }

    // "2"-like Z shape with rounded corners, inside 256x256 viewport
    private static List<Vector2> CreatePreviewGeometry()
    {
        const float cr = 24f;
        const float crD = 0.7071f * cr; // cr / √2 for 45° diagonal

        Vector2 topRight = new(196f, 68f);
        Vector2 topLeft = new(68f, 68f);
        Vector2 botRight = new(196f, 196f);
        Vector2 botLeft = new(60f, 196f);

        Vector2 c1Entry = new(topLeft.X + cr, topLeft.Y); // top-left corner entry
        Vector2 c1Exit = new(topLeft.X + crD, topLeft.Y + crD); // top-left corner exit (diagonal)
        Vector2 c2Entry = new(botRight.X - crD, botRight.Y - crD); // bot-right corner entry
        Vector2 c2Exit = new(botRight.X - cr, botRight.Y); // bot-right corner exit (left)

        var points = new List<Vector2>();
        points.Add(topRight);
        points.Add(c1Entry);
        AddBezier(points, c1Entry, topLeft, c1Exit);
        points.Add(c2Entry);
        AddBezier(points, c2Entry, botRight, c2Exit);
        points.Add(botLeft);
        return points;
    }

    private static void AddBezier(List<Vector2> points, Vector2 p0, Vector2 p1, Vector2 p2, int steps = 8)
    {
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            float u = 1 - t;
            points.Add(u * u * p0 + 2 * u * t * p1 + t * t * p2);
        }
    }
}