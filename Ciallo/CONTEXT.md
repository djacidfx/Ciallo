# Context

## Glossary

### Cel

A cel is a direct child layer of a cel folder, whether or not it is currently assigned to an exposure.

### Cel Button

A cel button is the clickable exposure bar on a CelTrack that represents one exposed cel at a specific timeline frame.

### Cel Folder

A cel folder is a folder layer whose children are cels. Cel folders do not nest inside other cel folders.

### Folder layer
Any layer's parent must be a folder layer. The document entity is a folder layer entity.

### Exposure

An exposure is a timeline assignment that says which cel is shown from a frame until the next exposure on the same cel folder.

### Frame Sequence

A frame sequence is an animation export made of one still image file per timeline frame.

### Onion Skin

Onion skin is a timeline viewing mode that shows nearby cel frames around the current frame as drawing references.

### Vector Fill Layer

A vector fill layer is a fill layer whose filled regions are bounded by reference layers.

### Reference Layer

A reference layer is a shape layer that provides boundary artwork for a vector fill layer.

### Shape

A shape is a selectable drawable object inside a shape-editable layer. The Select tool can select shapes independently of layer selection.

### Shape Clipboard

A shape clipboard is temporary copied or cut shape content that can later be pasted into a compatible working layer. It is distinct from layer-level copy and paste.

### Shape Paste

A shape paste creates new shapes from shape clipboard content in the current working layer. It is distinct from layer-level paste.

### Stroke Prediction

A stroke prediction is a transient visual extension of an in-progress stroke. It may be replaced by later input and is not saved or treated as committed drawing intent.

### Stroke Preview

A stroke preview is the user-visible form of an in-progress stroke. It may include stable stroke samples and transient stroke predictions.

### Paint Stroke Snap Target

A paint stroke snap target is the reference curve and curve-local hit position that a paint stroke endpoint may snap through when committed.

### Paint Stroke Snap Hint

A paint stroke snap hint is the user-visible dot that shows an available paint stroke snap target during hover or drawing.

### Gap Bridge

Gap Bridge repairs a visual gap by deforming a dangling endpoint of the source shape toward a target.

### Command History

Command history is a document-level record of undoable user changes.

### Undoable Action

An undoable action is one user-facing history entry that can be undone or redone as a unit. One undoable action may stay open long enough to gather multiple related command segments before the next action begins.

### Command Segment

A command segment is one ordered part of an undoable action. Related gestures may contribute multiple ordered command segments to the same undoable action.

## Relationships

- A **Vector Fill Layer** can have zero or more **Reference Layers**.
- A **Reference Layer** can provide boundary artwork for zero or more **Vector Fill Layers**.
- When editing reference artwork from a **Vector Fill Layer**, the edited **Shape** remains owned by its original **Reference Layer**.
