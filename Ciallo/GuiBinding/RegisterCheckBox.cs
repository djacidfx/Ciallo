using Godot;

namespace Ciallo;

/// <summary>
/// Register a Godot checkBox or checkBox-like button to command history.
/// </summary>
public static class RegisterCheckBox
{
    public static T RegisterUndo<T>(this T control, CommandManager manager, bool shouldAppendCommit = false) where T : Button
    {
        if (manager == null)
            return control;
        // block inner change to avoid infinite loop.
        bool innerChange = false;
        control.Toggled += toggleOn =>
        {
            bool shouldCommitToLatest = shouldAppendCommit;
            if (innerChange)
            {
                innerChange = false;
                return;
            }
            string actionName = "Toggle checkbox " + control.Name;
            var cmd = new DelegateCommand(
                () =>
                {
                    innerChange = true;
                    control.SetPressed(toggleOn);
                },
                () =>
                {
                    innerChange = true;
                    control.SetPressed(!toggleOn);
                });

            if (shouldCommitToLatest)
            {
                manager.CommitToLatest(actionName, cmd, execute: false);
            }
            else
            {
                manager.Commit(actionName, cmd, execute: false);
            }
        };
        return control;
    }
}
