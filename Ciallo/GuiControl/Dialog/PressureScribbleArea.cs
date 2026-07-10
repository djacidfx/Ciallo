using System;
using System.Collections.Generic;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// A throwaway scribble pad for feeling the global pen-pressure remap curve. Strokes are drawn
/// with the vanilla brush (solid black); per-point radius is interpolated between MinRadius and
/// MaxRadius by the <em>remapped</em> pen pressure, exactly as the canvas will render it. Nothing
/// is persisted or committed to any document. Lives only inside ConfigureGlobalPenPressure.
/// </summary>
/// <remarks>
/// Input is handled on this Control directly (raw sample points, no stroke modeler) so the drawn
/// width is a faithful readout of the curve. A child SubViewportContainer (Stretch, mouse Ignore)
/// maps this Control's local coordinates 1:1 onto viewport pixels, matching the brush-preview
/// node structure in <see cref="Ciallo.Command.NewStrokeBrushCmd"/>. Self-builds its viewport in
/// code like the other Ciallo widgets, so it drops into a scene as a bare Control + script.
/// </remarks>
[GlobalClass]
public partial class PressureScribbleArea : Control
{
    // radius = lerp(MinRadius, MaxRadius, mappedPressure); the floor keeps light touches visible.
    public readonly ReactiveProperty<float> MinRadius = new(1f);
    public readonly ReactiveProperty<float> MaxRadius = new(16f);

    // Fired on every pointer motion (drawing or just hovering): (rawPressure, mappedPressure).
    public event Action<float, float> PressureSampled;

    private SubViewport _viewport;
    private ShaderMaterial _material;

    private readonly List<StrokeView> _strokes = [];
    private StrokeView _current;
    private readonly List<Vector2> _points = [];
    private readonly List<float> _pressures = []; // remapped, one per point
    private bool _drawing;
    private float _lastMappedPressure;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;

        // Stretch maps the container (and this Control) rect 1:1 onto the viewport; Ignore lets
        // pointer events fall through to this Control's _GuiInput instead of into the viewport.
        var container = new SubViewportContainer { Stretch = true, MouseFilter = MouseFilterEnum.Ignore };
        container.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(container);

        _viewport = new SubViewport
        {
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            RenderTargetClearMode = SubViewport.ClearMode.Always,
            UseHdr2D = true,
            Disable3D = true,
        };
        container.AddChild(_viewport);

        var background = new ColorRect { Color = Colors.White, MouseFilter = MouseFilterEnum.Ignore };
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        _viewport.AddChild(background);

        // Vanilla black brush. Shader defaults already match, but set explicitly for clarity.
        _material = new ShaderMaterial { Shader = AutoloadRendering.StrokeShader };
        _material.SetShaderParameter("StrokeType", (int)BrushRenderingType.Vanilla);
        _material.SetShaderParameter("MaterialColor", Colors.Black);
        _material.SetShaderParameter("RadiusMode", 0); // world-space radius
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                if (button.Pressed) StartStroke(button.Position);
                else EndStroke();
                AcceptEvent();
                break;
            case InputEventMouseMotion motion:
                float raw = Mathf.Clamp(motion.Pressure, 0f, 1f);
                float mapped = Mathf.Clamp(AppPreference.PenPressureRemapCurve.Value.SampleX(raw), 0f, 1f);
                _lastMappedPressure = mapped;
                PressureSampled?.Invoke(raw, mapped);
                if (_drawing) AppendPoint(motion.Position, mapped);
                break;
        }
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        // Leaving the pad ends the current stroke so a released-outside pointer can't leave it dangling.
        if (what == NotificationMouseExit)
            EndStroke();
        else if (what == NotificationPredelete)
        {
            MinRadius.Dispose();
            MaxRadius.Dispose();
        }
    }

    private void StartStroke(Vector2 position)
    {
        _drawing = true;
        _points.Clear();
        _pressures.Clear();
        _current = new StrokeView { Material = _material };
        _viewport.AddChild(_current);
        // The button event carries no useful pressure; seed with the last hovered value.
        AppendPoint(position, _lastMappedPressure);
    }

    private void AppendPoint(Vector2 position, float mappedPressure)
    {
        _points.Add(position);
        _pressures.Add(mappedPressure);
        UpdateCurrentGeometry();
    }

    private void UpdateCurrentGeometry()
    {
        if (_current == null) return;
        float min = MinRadius.Value;
        float max = MaxRadius.Value;
        var radii = new float[_pressures.Count];
        for (int i = 0; i < radii.Length; i++)
            radii[i] = Mathf.Lerp(min, max, _pressures[i]);
        _current.SetGeometry(_points, radii, _pressures);
    }

    private void EndStroke()
    {
        if (!_drawing) return;
        _drawing = false;
        if (_current == null) return;
        _strokes.Add(_current); // freeze as-drawn; later slider changes only affect new strokes
        _current = null;
    }

    /// <summary>Removes every scribbled stroke, including one in progress.</summary>
    public void Clear()
    {
        EndStroke();
        foreach (var stroke in _strokes)
            stroke.QueueFree();
        _strokes.Clear();
        _points.Clear();
        _pressures.Clear();
    }
}
