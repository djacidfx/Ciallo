# Context

## Glossary

### Cel

A cel is a drawable layer inside a cel folder. A cel represents artwork that can be assigned to frames on the timeline.

### Exposure

An exposure is a timeline assignment that says which cel is shown from a frame until the next exposure on the same cel folder.

### Command History

Command history is a document-level record of undoable user changes.

### Undoable Action

An undoable action is one user-facing history entry that can be undone or redone as a unit. One undoable action may stay open long enough to gather multiple related command segments before the next action begins.

### Command Segment

A command segment is one ordered part of an undoable action. Related gestures may contribute multiple ordered command segments to the same undoable action.
