using System.Threading.Tasks;
using Ciallo.Misc;
using Godot;

public partial class SaveChangeDialog : ConfirmationDialog
{
    public TaskCompletionSource<int> DialogResultSource;
    public readonly Button YesButton;
    public readonly Button NoButton;

    public SaveChangeDialog()
    {
        DialogText = "Save changes?".Tr();
        NoButton = AddButton("No");
        YesButton = AddButton("Yes");
        GetOkButton().Visible = false;

        YesButton.Pressed += OnYes;
        NoButton.Pressed += OnNo;
        Canceled += OnCancel;
    }

    public Task<int> PopupCollectInput()
    {
        DialogResultSource = new TaskCompletionSource<int>();
        PopupCentered();

        return DialogResultSource.Task;
    }

    private void OnNo()
    {
        DialogResultSource?.SetResult(0);
        DialogResultSource = null;
        Hide();
    }

    public void OnYes()
    {
        DialogResultSource?.SetResult(1);
        DialogResultSource = null;
        Hide();
    }

    public void OnCancel()
    {
        DialogResultSource?.SetResult(-1);
        DialogResultSource = null;
        Hide();
    }
}