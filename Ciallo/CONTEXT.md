# Context

## Glossary

### Cel

A cel is a drawable layer inside a cel folder. A cel represents artwork that can be assigned to frames on the timeline.

### Cel Button

A cel button is the clickable exposure bar on a CelTrack that represents one exposed cel at a specific timeline frame.

### Cel Folder

A cel folder is a folder layer whose children are cels. Cel folders do not nest inside other cel folders.

### Folder layer
Any layer's parent must be a folder layer. The document entity is a folder layer entity.

### Exposure

An exposure is a timeline assignment that says which cel is shown from a frame until the next exposure on the same cel folder.

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

### Command History

Command history is a document-level record of undoable user changes.

### Undoable Action

An undoable action is one user-facing history entry that can be undone or redone as a unit. One undoable action may stay open long enough to gather multiple related command segments before the next action begins.

### Command Segment

A command segment is one ordered part of an undoable action. Related gestures may contribute multiple ordered command segments to the same undoable action.
