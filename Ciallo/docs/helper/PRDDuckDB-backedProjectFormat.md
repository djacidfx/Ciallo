# PRD: DuckDB-backed Project Format

## Problem Statement

Ciallo currently stores project data in a SQLite-backed format, but many semantically meaningful creative objects are still stored as MessagePack blobs. This is sufficient for single-document save and load, but it makes project data hard to inspect, edit, query, and summarize for developers, technical artists, and future pipeline tooling.

Ciallo is primarily a CSP-like animation production tool. Industrial animation workflows often need to reason across clips and project files: finding brush usage, batch exporting clips, replacing brushes, and generating production statistics. Ciallo also contains substantial 2D geometry data, so future spatial or GIS-adjacent tooling may benefit from the DuckDB ecosystem.

This first phase does not implement cross-project features. It changes the underlying project format so Ciallo project files become a better foundation for future production analysis while keeping single-document drawing save/load as the highest priority.

## Solution

Switch Ciallo project files to a DuckDB-backed format. A `.ciallo` file should itself be a DuckDB database file, not a zip container that wraps a database.

The user-visible behavior of opening, saving, and saving-as a project remains unchanged. Regular users do not need to know that the storage backend changed.

The schema is a project format contract. It should roughly mirror the persisted Component Class structure, but it must not be a raw dump of C# reflection fields. Field names should be stable, readable, and reflect Ciallo domain concepts.

## Goals

1. Preserve existing single-document open, save, and save-as workflows.
2. Replace the current SQLite-backed project storage with DuckDB-backed storage.
3. Remove the zip container; the `.ciallo` file itself is the database file.
4. Reduce unnecessary MessagePack blobs by storing creative semantic data with DuckDB structured types.
5. Preserve a path toward future cross-clip and cross-project production analysis.
6. Avoid adding new user-visible cross-project features in this phase.

## Non-Goals

1. Do not read the old SQLite project format.
2. Do not provide forward or migration compatibility; Ciallo has not shipped this format publicly yet.

## Business Rationale

Ciallo project files should not remain only private documents that Ciallo can open and save. Long term, they should also serve as structured creative production data that future industrial animation pipeline tools can inspect and analyze.

The priority is explicit: single-file drawing save/load always comes before analysis convenience. No format decision should significantly harm save reliability, load reliability, file integrity, or the regular drawing workflow.

DuckDB is valuable here because:

1. It supports rich structured data types such as `STRUCT`, `LIST`, and `MAP`, which are a good fit for creative data such as `Color`, `Vector2`, Bezier curves, and brush parameters.
2. It supports nested structures, allowing saved data to stay close to the shape of Component Classes while remaining inspectable through SQL.
3. It gives Ciallo a stronger foundation for future cross-file querying, aggregation, and production tooling.
4. It has potential synergy with 2D geometry and GIS-adjacent workflows, though GIS functionality is not a first-phase deliverable.

## Data Design Principles

1. Component tables should roughly correspond to persisted Component Classes.
2. Component fields should roughly correspond to DuckDB columns.
3. Project-format infrastructure tables such as `metadata` and `entities` are allowed.
4. Entity references, entity lists, and entity maps should preserve the current SQLite format semantics.
5. Blobs should be reserved for true binary media.
6. Creative semantic data should be stored structurally whenever practical.
7. Do not add derived summary fields in this phase.
8. Do not add extra analysis tables in this phase.
9. The schema is a project format contract, not a dump of C# implementation details.

## Structured Data Scope

The first phase should store these data categories structurally:

1. `Color`
2. `Vector2`
3. `Transform2D`
4. Bezier curves
5. Brush parameters
6. Stroke geometry positions, pressures, radii, and tilts

These data categories may remain blobs:

1. Images
2. Textures
3. Stamp and mask images
4. Other resources that are inherently binary media

## Stroke Geometry Decision

Stroke geometry must not be stored as one row per sample point.

The first phase should store stroke geometry at stroke/component granularity. Point data should use DuckDB nested types or compact array-like structures. This keeps project files aligned with the single-file drawing save/load priority and avoids inflating file size and row counts.

Raw point columns may remain expandable for technical inspection, but point-level cross-file analytics are not a first-phase goal.

## Brush Matching Assumption

Future cross-project brush search or replacement may treat brushes with the same name as the same business-level brush category.

This is a production workflow convention, not a guaranteed unique identity. The first phase only needs to preserve enough structured brush data to make this future query possible. It does not implement search or replacement behavior.

## Acceptance Criteria

1. Ciallo can save a document into the new DuckDB-backed `.ciallo` format.
2. Ciallo can load a saved DuckDB-backed `.ciallo` document with equivalent document state.
3. Open, save, and save-as workflows remain unchanged from the user's perspective.
4. `.ciallo` is no longer a zip container.
5. The project schema roughly mirrors the persisted Component Class structure.
6. Creative semantic fields are not unnecessarily hidden in MessagePack blobs.
7. Binary media remains stored as blobs where appropriate.
8. Stroke geometry is not saved as one row per sample point.
9. Old SQLite format compatibility is not required.
10. No new cross-file user-facing feature is required.