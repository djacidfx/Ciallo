using Ciallo.GuiBinding;
using Godot;

namespace Ciallo.NodeControl;

public partial class RecentDocumentList : ItemList
{
    public override void _Ready()
    {
        this.ObserveObservableList(AppPreference.RecentFiles);
        ItemActivated += idx =>
        {
            var path = AppPreference.RecentFiles[(int)idx];
            var success = OpenDocumentDialog.LoadWorldFile(path);
            if (!success) AppPreference.RecentFiles.Remove(path);
        };

        ItemClicked += (index, _, buttonIndex) =>
        {
            if ((MouseButton)buttonIndex == MouseButton.Right)
            {
                AppPreference.RecentFiles.RemoveAt((int)index);
            }
        };
    }
}