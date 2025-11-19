using System.Threading.Tasks;
using Godot;

public partial class YesNoDialog : ConfirmationDialog
{
    private TaskCompletionSource<int> _dialogResultSource;
    public readonly Button YesButton;
    public readonly Button NoButton;

    public YesNoDialog()
    {
        NoButton = AddButton("No");
        YesButton = AddButton("Yes");
        GetOkButton().Visible = false;

        YesButton.Pressed += OnYes;
        NoButton.Pressed += OnNo;
        Canceled += OnNo;
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
}