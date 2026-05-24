using System.Collections.Generic;
using Ciallo.Geometry;
using Godot;

namespace Ciallo.Data;

public static partial class AppStrokeBrushLibrary
{
    public static List<StrokeBrushSetting> CreateBuiltInBrushes()
    {
        List<StrokeBrushSetting> brushes = [];
        brushes.Add(new()
        {
            Name = { Value = $"{"Solid".Tr()} {"G-pen".Tr()}" },
            RenderingType = { Value = BrushRenderingType.Vanilla },
            Pressure2RadiusCurve = { Value = BezierCurveFactory.GPenCurve(0.2f, 1.0f) },
            Labels = { BrushLabel.BuiltIn },
        });

        brushes.Add(new()
        {
            Name = { Value = "G-pen".Tr() },
            RenderingType = { Value = BrushRenderingType.Airbrush },
            ActiveBrushFlags = { Value = BrushFlags.Pressure2Flow },
            Pressure2RadiusCurve = { Value = BezierCurveFactory.GPenCurve(0.1f, 1.0f) },
            Pressure2FlowCurve = { Value = BezierCurveFactory.EaseInOut(new Vector2(0.0f, 0.1f), new(0.75f, 1.0f)) },
            FalloffCurve = { Value = BezierCurveFactory.Constant(1.0f) },
            AlphaDensity = { Value = 8 },
            Labels = { BrushLabel.BuiltIn },
        });

        brushes.Add(new()
        {
            Name = { Value = "Mapping pen".Tr() },
            RenderingType = { Value = BrushRenderingType.Airbrush },
            ActiveBrushFlags = { Value = BrushFlags.Pressure2Flow },
            Pressure2RadiusCurve = { Value = BezierCurveFactory.GPenCurve(0.1f, 1.0f) },
            Pressure2FlowCurve =
            {
                Value =
                [
                    new(new(0.4f, 0.1f), new(-0.3f, 0), new(0.3f, 0)),
                    new(new(0.75f, 1.0f), new(-0.1f, 0), new(0.1f, 0))
                ]
            },
            FalloffCurve = { Value = BezierCurveFactory.Constant(1.0f) },
            AlphaDensity = { Value = 8 },
            Labels = { BrushLabel.BuiltIn },
        });

        brushes.Add(new()
        {
            Name = { Value = "Eraser".Tr() },
            RenderingType = { Value = BrushRenderingType.Vanilla },
            BaseRadius = { Value = 12f },
            ActiveBrushFlags = { Value = BrushFlags.Eraser },
            Pressure2RadiusCurve = { Value = BezierCurveFactory.EaseInOut(0.8f, 1.0f) },
            Labels = { BrushLabel.BuiltIn },
        });

        brushes.Add(new()
        {
            Name = { Value = "Soft eraser".Tr() },
            RenderingType = { Value = BrushRenderingType.Airbrush },
            BaseRadius = { Value = 12f },
            Labels = { BrushLabel.BuiltIn },
            Color = { Value = new(0, 0, 0, 0.4f) },
            ActiveBrushFlags = { Value = BrushFlags.Pressure2Flow | BrushFlags.Eraser },
            Pressure2FlowCurve = new(BezierCurveFactory.EaseInOut()),
            FalloffCurve = new([
                new(new(0, 1), new(-0.25f, 0), new(0.5f, 0)),
                new(new(1, 0), new(-0.25f, 0), new(0.25f, 0))
            ]),
        });

        brushes.Add(new()
        {
            Name = { Value = "Soft airbrush".Tr() },
            RenderingType = { Value = BrushRenderingType.Airbrush },
            BaseRadius = { Value = 12f },
            Labels = { BrushLabel.BuiltIn },
            Color = { Value = new(0, 0, 0, 0.4f) },
            ActiveBrushFlags = { Value = BrushFlags.Pressure2Flow },
            Pressure2FlowCurve = new(BezierCurveFactory.EaseInOut()),
            FalloffCurve = new([
                new(new(0, 1), new(-0.25f, 0), new(0.5f, 0)),
                new(new(1, 0), new(-0.25f, 0), new(0.25f, 0))
            ]),
        });

        brushes.Add(new()
        {
            Name = { Value = "Hard airbrush".Tr() },
            RenderingType = { Value = BrushRenderingType.Airbrush },
            BaseRadius = { Value = 12f },
            Labels = { BrushLabel.BuiltIn },
            Color = { Value = new(0, 0, 0, 0.9f) },
            ActiveBrushFlags = { Value = BrushFlags.Pressure2Flow },
            Pressure2FlowCurve = new(BezierCurveFactory.EaseInOut()),
            FalloffCurve = new([
                new(new(0, 1), new(-0.25f, 0), new(0.65f, 0)),
                new(new(1, 0), new(0, 0.25f), new(0.25f, 0))
            ]),
        });

        var dirPath = "res://Rendering/Image/";
        Image[] images =
        [
            GD.Load<Image>(dirPath + "StampPencil.png"),
            GD.Load<Image>(dirPath + "StampSplatter.png"),
            GD.Load<Image>(dirPath + "FBMNoise.png")
        ];
        foreach (var image in images)
        {
            image.GenerateMipmaps();
        }

        brushes.Add(new()
        {
            Name = { Value = "Pencil".Tr() },
            RenderingType = { Value = BrushRenderingType.Stamp },
            Labels = { BrushLabel.BuiltIn },
            Color = { Value = Colors.Black },
            ActiveStampFlags = { Value = StampFlags.StampTexture | StampFlags.MaskTexture | StampFlags.RotationNoise },
            StampTexture = { Value = ImageTexture.CreateFromImage(images[0]) },
            StampInterval = { Value = 0.25f },
            MaskTexture = { Value = ImageTexture.CreateFromImage(images[2]) },
            RotationNoiseAmplitude = { Value = 8 * Mathf.Pi },
        });

        brushes.Add(new()
        {
            Name = { Value = "Splatter".Tr() },
            RenderingType = { Value = BrushRenderingType.Stamp },
            Labels = { BrushLabel.BuiltIn },
            ActiveStampFlags = { Value = StampFlags.StampTexture | StampFlags.RotationNoise },
            StampTexture = { Value = ImageTexture.CreateFromImage(images[1]) },
            RotationNoiseAmplitude = { Value = Mathf.Pi },
        });

        return brushes;
    }
}