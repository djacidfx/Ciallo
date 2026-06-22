# Ciallo Render Exchange Format Requirements

## Status

This note records the current product-level requirements for a future Ciallo render exchange format. It is not an implementation plan and does not choose the final file extension, binary layout, text grammar, or exact chunk/tag names yet.

The working mental model is a small display-oriented graphics format, closer to `png`, `gif`, or `mp4` in purpose than to `svg` or a Ciallo project file.

## Goal (TL;DR)

- Export Ciallo-defined **stroke**, **fill** content and their organizers **layer**, **frame** into a simple 2D render exchange format.
- Let game engines import the file as one sprite animation clip, or as a single sprite when it contains one frame.
- Keep the format render/display oriented, not editable like SVG and not a Ciallo project/archive format.
- Prefer binary for the production format, while leaving room for a text/debug representation if it proves useful.
- Make the format parseable by a small custom loader.
- Do not require CPU rendering. Stroke rendering is expected to be GPU/shader based.

## Core Business Model

The current preferred model is:

```text
clip
  frames[]
    paint tree in painter order
```

- One file represents one clip.
- A one-frame clip is a single sprite.
- A multi-frame clip is a frame-by-frame sprite animation.
- Each frame is a complete display snapshot, not a delta from the previous frame.
- Paint order follows the SVG painter model: later siblings are painted over earlier siblings.

## Paint Nodes

The frame content is a tree of paint nodes:

```text
paint_node = group | draw_unit
```

### Group

A `group` is the preferred name for layer-like render grouping.

- A group is a render/compositing concept, not a Ciallo editor-layer object.
- Group order participates in the same painter model as draw units.
- Group opacity should mean whole-group opacity, like an SVG `<g opacity="...">`, rather than simply multiplying each child independently.
- Group settings should be represented as render information, not geometry.
- Group metadata such as a display/debug name should stay separate from render information.

The format should preserve group/layer rendering semantics where they affect the final visual result, especially for galgame-style rendering. It should not preserve editor-only settings such as locked state, selection, mark color, tree expansion state, or other Ciallo UI details.

Hidden or non-rendered source content should normally be omitted from export instead of being preserved as disabled content.

### Draw Unit

A `draw_unit` is an independent drawable primitive. It is not primarily a material batch and it should not imply stable identity across frames.

The current required draw unit kinds are:

- `stroke_polyline`
- `triangle_mesh_2d`

Stable object/chunk IDs are not part of the current business requirement. If layer/group semantics are preserved, they should serve rendering/compositing rather than object identity.

## Geometry Requirements

### Stroke

Stroke must remain a native polyline-based primitive. Exporting stroke as a triangle mesh is not an acceptable primary representation.

The stroke primitive should be based on Ciallo's polyline geometry model:

- positions
- radii
- pressures
- tilts

The exact stroke shader contract still needs a formal definition. The format should define a clear GPU/shader-level stroke model rather than embedding arbitrary engine-specific shader source code.

### Fill

Fill should be exported as triangulated polygon geometry.

The format does not need to preserve editable polygon rings, fill rules, or SVG-like path semantics as primary data. The exported fill result should be directly usable as a triangle mesh by a renderer.

## Render Information

Geometry and render information should be separate concepts.

```text
draw_unit
  geometry
  render_info
  metadata

group
  render_info
  metadata
  children
```

Render information is the future home for:

- opacity
- blend mode
- stroke/fill material references
- shader/stroke model references
- texture/color information if material support is added later

Metadata is for non-rendering information such as optional names or debug labels.

Material and color completeness is not a v1 business requirement. If material information is absent, callers may render the geometry as black or choose their own fallback appearance.

## Layer And Group Policy

The current direction is to preserve SVG-style group semantics, not Ciallo's full layer data model.

- Keep group/layer compositing where it affects display.
- Do not use layers as stable cross-frame identity.
- Do not preserve Ciallo editor-only layer settings.
- Treat each frame as its own complete render tree.
- Use group opacity only as render information.

This keeps the format close to a display format while still preserving the layer/group semantics needed for galgame-style compositing.

## Format Definition Requirements

The final syntax/container is intentionally not decided yet. However, the format definition itself must satisfy these requirements:

- A parser author should immediately be able to tell whether a file is binary or text.
- The file should have a clear signature/magic header.
- The format should make its closest technical model obvious, so readers can infer the relevant parsing strategy.
- The format should have an obvious answer for how records are separated.
- The format should have an obvious answer for how tags/chunks are named and interpreted.

The user requirement to preserve for future format design:

> A parser author should be able to look at the file and immediately understand whether it is binary or text, what family of related technologies can be used to read it, and which closest existing format model gives it credibility.

Current naming/container ideas are only candidates:

- `.pngvg` / "PNG Vector Graphics" communicates a PNG-like vector graphics idea, but may imply unwanted PNG compatibility.
- `.pvg2d` / "Portable Vector Graphics 2D" is another possible name if the format should be clearly independent from PNG.
- A PNG/RIFF-style binary chunk container is currently the closest technical model under discussion.
- SVG's painter/group model is the closest rendering semantics under discussion.
- PLY-like typed arrays are a possible influence for geometry payloads, but literal PLY compatibility is not a current goal.

If the production format is binary, records should probably be chunks rather than text lines. A future text/debug form could mirror the same tags in a line-oriented syntax, but that is not yet a committed requirement.

## Open Decisions

- Final format name and extension.
- Binary-only production format versus paired binary/text forms.
- Exact magic header/signature.
- Exact chunk/tag grammar.
- Whether chunks are flat with begin/end tags or nested length-delimited containers.
- Exact render info fields for v1.
- Exact group opacity/compositing contract and acceptable fast-path approximations.
- Exact shader-level definition of `stroke_polyline`.
- Whether future material/style tables are mandatory, optional, or extension chunks.
