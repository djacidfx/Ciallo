using Ciallo.Widget;
using Godot;

namespace Ciallo.GuiControl;

public partial class WelcomePanel : PanelContainer
{
    public override void _Ready()
    {
        var newDocumentButton = GetNode<Button>("%NewDocumentButton");
        var openDocumentButton = GetNode<Button>("%OpenDocumentButton");
        var recentList = GetNode<ItemList>("%RecentList");
        var uiScaleSlider = GetNode<SpinSlider>("%SpinSlider");

        newDocumentButton.Pressed += AppGuiCommand.PopupNewDocumentDialog;
        openDocumentButton.Pressed += AppGuiCommand.PopupOpenDocumentDialog;

        recentList.ObserveObservableList(AppPreference.RecentFiles);
        recentList.ItemActivated += idx =>
        {
            var path = AppPreference.RecentFiles[(int)idx];
            var success = OpenDocumentDialog.LoadWorldFile(path);
            if (!success) AppPreference.RecentFiles.Remove(path);
        };
        recentList.ItemClicked += (index, _, buttonIndex) =>
        {
            if ((MouseButton)buttonIndex == MouseButton.Right)
                AppPreference.RecentFiles.RemoveAt((int)index);
        };

        uiScaleSlider.BindNumber(AppPreference.UIScale);
    }
}