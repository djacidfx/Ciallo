# Cel child archetypes

A cel folder's children are *cels* (the per-frame drawings). Within those cels, layers are grouped by name into **archetypes**: every cel child sharing a name belongs to one archetype (`FolderLayerSetting.CelChildrenByName`, name → set of layers). The whole archetype system is name-keyed — `CelChildrenByName`, `PreferredNameForCelSelection`, `GetLayerChildByName` all address cel children by name (see memory `cel-child-preferred-name`). A name, not a path or an entity, is the stable identity of "the same layer across cels."

Two operations act on archetypes: editing the layers of an existing group, and generating a new cel from the group structure. Both are described below.

## Archetypes hold no state of their own

A archetype block stores no value. Its display binds to one representative member of the group, and user input pushes the new value onto every member in one undoable action (blind overwrite). Layers never sync back through an archetype.

An earlier design cloned values onto the archetype, creating a second source of truth that undo/redo did not touch: toggling an archetype then undoing left the members reverted but the archetype still showing the old value, and the drift compounded on the next batch edit. Binding the display straight to a member makes the layers the single ground truth — undo reverts members, the representative's property fires, the display follows, and the block can never disagree with reality.

## New cels replicate the archetype schema

A new cel is not a bare folder with one blank layer; it replicates the archetype schema. For each archetype name it creates one same-named child, cloned from that name's representative (the same first-member representative the archetype block binds to). The entire build — folder, children, reference remap, working-layer pick, exposure, playhead — is one undoable command in `TimelineAction.NewCelFromArchetype`, shared by both entry points (toolbar button and cel right-click menu, which differ only in how they pick frame and name).

- **One child per name, representative-cloned, non-recursive.** Cloning goes through the existing `NewXxxLayer(copyE)` path, so `CommonLayerSetting` and the type-specific setting come over for free. A folder representative is cloned shallowly — its descendants are not archetype members, so the schema stops at the cel's direct children. Children are appended in ascending representative `LayerTreeNode.Index`, reproducing the source cel's layer order.

- **Vector fill references are rebuilt by name.** A fill layer's `ReferenceLayers` are cel-local shape siblings. Copying the representative's reference list verbatim would keep pointing back to the source cel, which is wrong for every copy-based creation path. Instead `NewCelFromArchetype` remaps each reference by the child's name inside the new cel, preserving links to same-named siblings and intentionally dropping references that cannot be represented there.
  This is why `NewVectorFillLayerCmd`'s copy path clears `ReferenceLayers` outright: every copy-based creation (deserialization, cel-from-archetype) rebuilds references itself, so carrying the source's over is always wrong. Clearing also retired the old cross-`World` reference guard that only existed to paper over the same mistake.
