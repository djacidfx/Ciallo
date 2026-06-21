# Cel child templates

A cel folder's children are *cels* (the per-frame drawings). Within those cels, layers are grouped by name into **templates**: every cel child sharing a name belongs to one template (`FolderLayerSetting.CelChildrenByName`, name → set of layers). The whole template system is name-keyed — `CelChildrenByName`, `PreferredNameForCelSelection`, `GetLayerChildByName` all address cel children by name (see memory `cel-child-preferred-name`). A name, not a path or an entity, is the stable identity of "the same layer across cels."

Two operations act on templates: editing the layers of an existing group, and generating a new cel from the group structure. Both are described below.

## Templates hold no state of their own

A template block stores no value. Its display binds to one representative member of the group, and user input pushes the new value onto every member in one undoable action (blind overwrite). Layers never sync back through a template.

An earlier design cloned values onto the template, creating a second source of truth that undo/redo did not touch: toggling a template then undoing left the members reverted but the template still showing the old value, and the drift compounded on the next batch edit. Binding the display straight to a member makes the layers the single ground truth — undo reverts members, the representative's property fires, the display follows, and the block can never disagree with reality.

Consequences:

- The display reflects one representative, not the group as a whole. Heterogeneous members (one hidden, one visible) show the representative's state, not a "mixed" indicator; a batch edit then overwrites all of them to one value, which is intended.
- The **representative** is any current member, re-picked on every reconcile (a name appearing or vanishing anywhere in the folder). A rename that merges into an existing name keeps that name's block and re-binds it to a representative of the merged group, so display and push always reflect current membership. *Not* re-picked: a member added to or removed from a surviving name with no key change — the inner set's signals are unused today. Re-pick on inner-set change if that membership ever becomes user-visible.

## New cels replicate the template schema

A new cel is not a bare folder with one blank layer; it replicates the template schema. For each template name it creates one same-named child, cloned from that name's representative (the same first-member representative the template block binds to). The entire build — folder, children, reference remap, working-layer pick, exposure, playhead — is one undoable command in `TimelineAction.NewCelFromTemplate`, shared by both entry points (toolbar button and cel right-click menu, which differ only in how they pick frame and name).

Replication is name-keyed for the same reason everything else is: it keeps one addressing model instead of introducing a fourth.

- **One child per name, representative-cloned, non-recursive.** Cloning goes through the existing `NewXxxLayer(copyE)` path, so `CommonLayerSetting` and the type-specific setting come over for free. A folder representative is cloned shallowly — its descendants are not template members, so the schema stops at the cel's direct children. Children are appended in ascending representative `LayerTreeNode.Index`, reproducing the source cel's layer order.

- **Singleton filter past bootstrap.** Once the folder holds more than one cel, a name owned by a single layer is a one-off (a stray layer in one cel, not shared schema) and is skipped. While the folder still has ≤1 cel every name is necessarily single-membered, so the filter is disabled there — otherwise the folder's *first* generated cel would come out empty. The count is over direct cel children (folder children), the same population `CelChildrenByName` aggregates, so the filter's denominator matches what it filters.

- **Two fallbacks, deliberately distinct.** An *empty* `CelChildrenByName` (the folder's first cel — no schema yet) seeds one blank shape layer to draw on, preserving the original bootstrap behavior. A *non-empty* dictionary that filters down to nothing produces an empty cel folder with no fallback — the schema said "nothing qualifies," and inventing a layer would contradict it.

- **Fill references remap by name.** A fill layer's references are cel-local (it points at shape siblings inside its own cel), so a blind clone would leave the new fill pointing at the *source* cel's siblings. Instead, after all children exist, each new fill layer's references are rebuilt from its representative's list, each reference translated name → same-named child in this cel. An unmatched reference is kept only if it has no cel-folder ancestor (a document-level shared layer, legitimately cross-cel); if it lives inside some cel it is dropped rather than left pointing across cels into another frame's drawing.

  This is why `NewVectorFillLayerCmd`'s copy path clears `ReferenceLayers` outright: every copy-based creation (deserialization, cel-from-template) rebuilds references itself, so carrying the source's over is always wrong. Clearing also retired the old cross-`World` reference guard that only existed to paper over the same mistake.

- **Working layer follows the cel-button rule.** The working layer resolves to the new cel's child matching `PreferredNameForCelSelection`, exactly as clicking a cel button or scrubbing does. No match (empty preference, or that name was filtered out) leaves the working layer untouched — the same "rather not select than select wrong" stance as cel navigation. The blank-shape bootstrap is the lone exception: it selects its one new layer, since there is no preference to honor yet.
