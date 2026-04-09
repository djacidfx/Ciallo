using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.GuiControl;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

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
        Vector2I size = new(256, 128);
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
        float gv = 16;
        var previewRect = new Rect2(Vector2.Zero, size).GrowIndividual(-gv, -2*gv, -gv, -2*gv);
        _setting.BaseRadius.CombineLatest(_setting.Pressure2RadiusCurve.Changed.Prepend(Unit.Default), (r, _) => r)
            .Subscribe(r =>
            {
                int n = 32;
                float pi = Mathf.Pi;
                var radius = Enumerable.Range(0, n)
                    .Select(i => Mathf.Lerp(-pi / 2, pi / 2, (float)i / (n - 1)))
                    .Select(Mathf.Cos) // pen pressure
                    .Select(_setting.Pressure2RadiusCurve.SampleX)
                    .Select(t => t * r.SigmoidRemap(5, 32, 6, 32))
                    .ToArray();
                previewStroke.SetGeometry(CreatePreviewGeometry(previewRect, n), radius);
            });

        // background
        var bg = new ColorRect
        {
            Size = new Vector2(size.X, size.Y),
            Material = AutoloadRendering.CheckerboardMaterial,
            Color = Colors.Transparent,
        };
        viewport.AddChild(bg);
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


    private static List<Vector2> CreatePreviewGeometry(Rect2 dst, int numPoints = 32)
    {
        var points = new List<Vector2>(numPoints);

        float pi = Mathf.Pi;
        // x ∈ [-π, π], y ∈ [-1, 1]
        Rect2 src = new Rect2(-pi, -1f, 2f * pi, 2f);
        Vector2 scale = dst.Size / src.Size;
        Vector2 srcCenter = src.GetCenter();
        Vector2 dstCenter = dst.GetCenter();
        Transform2D transform = new Transform2D(0f, scale, 0f, dstCenter - srcCenter * scale);

        for (int i = 0; i < numPoints; i++)
        {
            float x = Mathf.Lerp(-pi, pi, (float)i / (numPoints - 1));
            float y = Mathf.Sin(x);
            points.Add(transform * new Vector2(x, y));
        }
        return points;
    }
}