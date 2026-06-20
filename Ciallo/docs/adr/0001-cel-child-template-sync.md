# Cel child templates hold no state of their own

Cel child templates batch-edit cel child layers grouped by name. A template block stores no value: its display binds to one representative member of the group, and user input pushes the new value onto every member in one undoable action (blind overwrite, per-layer snapshot). Layers never sync back through a template.

We deliberately gave the template no value of its own. An earlier design cloned the values onto the template, which created a second source of truth that undo/redo did not touch — toggling a template, then undoing, left the members reverted but the template still showing the old value, and the drift compounded on the next batch edit. Binding the display straight to a member makes the layers the single ground truth: undo reverts members, the representative's property fires, the display follows, and the block can never disagree with reality.

Consequences:
- The display reflects one representative member, not the group as a whole. If members hold heterogeneous values (one hidden, one visible), the block shows the representative's state, not a "mixed" indicator. A batch edit then overwrites all of them to one value, which is the intended behavior.
- The representative is any current member, re-picked on every reconcile (a key appearing or vanishing anywhere in the folder). A rename that merges into an existing name keeps that name's block and re-binds it to a representative of the merged group, so display and push always reflect the same current membership. What is *not* re-picked is a member added to or removed from a surviving name with no key change — the inner set's signals are unused today; re-pick on inner-set change if that membership ever becomes user-visible.
