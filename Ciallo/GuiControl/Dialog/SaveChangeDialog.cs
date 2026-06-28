using System.Threading.Tasks;
using Godot;

namespace Ciallo.GuiControl;

public partial class SaveChangeDialog : ConfirmationDialog
{
    private TaskCompletionSource<int> _dialogResultSource;
    public readonly Button YesButton;
    public readonly Button NoButton;

    public SaveChangeDialog()
    {
        DialogText = "Save changes?".Tr();
        NoButton = AddButton("No".Tr());
        YesButton = AddButton("Yes".Tr());
        GetOkButton().Visible = false;

        YesButton.Pressed += OnYes;
        NoButton.Pressed += OnNo;
        Canceled += OnCancel;
    }

    public Task<int> PopupCollectInput()
    {
        _dialogResultSource = new TaskCompletionSource<int>();
        PopupCentered();

        return _dialogResultSource.Task;
    }

    private void OnNo()
    {
        _dialogResultSource?.SetResult(0);
        _dialogResultSource = null;
        Hide();
    }

    public void OnYes()
    {
        _dialogResultSource?.SetResult(1);
        _dialogResultSource = null;
        Hide();
    }

    public void OnCancel()
    {
        _dialogResultSource?.SetResult(-1);
        _dialogResultSource = null;
        Hide();
    }
}
