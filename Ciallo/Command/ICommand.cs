using System.Collections.Generic;
using Frent;
using Godot;

namespace Ciallo.Command;

public interface ICommand
{
    public void Do();
    public void Undo();

    /// <summary>
    /// `DoRefEntities` are the entities will be destroyed when this command is ready to redo and deleted.
    /// e.g. User undo the most recent command, then clear the whole history. So the most recent command satisfies the above statement.
    /// Entity version of `add_do_reference`.
    /// </summary>
    public IEnumerable<Entity> DoRefEntities { get; }
    public IEnumerable<Entity> UndoRefEntities { get; }

    public IEnumerable<GodotObject> DoRefObjects { get; }
    public IEnumerable<GodotObject> UndoRefObjects { get; }
}