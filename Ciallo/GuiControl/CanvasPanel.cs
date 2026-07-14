using Godot;

namespace Ciallo.GuiControl;

public partial class CanvasPanel : Container
{
    private WelcomePanel WelcomePanel => GetNode<WelcomePanel>(nameof(WelcomePanel));

    public void ShowDocument(PaintPanel paintPanel)
    {
        WelcomePanel.Hide();
        AddChild(paintPanel);
    }

    public void ShowWelcome()
    {
        WelcomePanel.Show();
    }
}
