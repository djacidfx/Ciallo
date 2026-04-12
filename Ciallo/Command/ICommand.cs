namespace Ciallo.Command;

public interface ICommand
{
    public void Do();
    public void Undo();
    // When a command is about to be deleted, it could be ready to Do or Undo, corresponding to OnDeletedAsDo/Undo
    // When deleted as do, it should clean up the entities or Godot nodes created by the command.
    public void OnDeletedAsDo() { }
    public void OnDeletedAsUndo() { }
}