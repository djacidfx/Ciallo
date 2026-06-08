# Ink Stroke Modeler Migration Plan

## Decisions

- Port the stroke modeler as a pure C# `InkStrokeModeler` class library.
- Keep the modeler independent from Godot and Ciallo types.
- Use rnote-style defaults through `StrokeModelParams.CreateRnoteDefault()`.
- Use `StrokeEnd` prediction by default.
- Port Kalman prediction and loop contraction mitigation, but keep both non-default.
- Do not keep the C++ reference clone, C++ exporters, golden fixtures, CMake, or Bazel files.
- `PolylineInteractiveGenerator.CurrentGeometry` is a short-lived internal-buffer view.
- `Update()` may expose stable samples plus transient prediction; `End()` exposes stable samples only.

## Current Integration

- `PolylineInteractiveGenerator` is the Ciallo adapter.
- Cursor world position is passed to the modeler.
- Modeler time is stroke-local elapsed seconds.
- Modeler tilt and orientation are unknown in v1; Ciallo still stores `Vector2` tilt from raw cursor samples.
- The button-down pressure is delayed until the first real motion pressure.
- Paint stroke and vector-fill stroke paths consume only `CurrentGeometry`.

## Tests

- Default/rnote parameter tests.
- Parameter validation tests.
- Modeler lifecycle tests.
- Prediction transient-state tests.
- Kalman smoke test with tuned parameters.

## Follow-Up

- Validate drawing feel on real stylus hardware.
- Decide whether loop contraction mitigation should become enabled for any tool preset.
- Decide whether Kalman prediction should be exposed as an experimental brush/tool setting.
- Revisit taper ending only after testing the modeler-driven end-of-stroke behavior.
