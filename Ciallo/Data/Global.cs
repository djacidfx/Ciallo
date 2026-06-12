global using static Ciallo.Data.Global;

using Ciallo.GuiControl;

namespace Ciallo.Data;

public static partial class Global
{
    public static readonly Preference AppPreference = new();
    public static DialogHost AppDialogHost;
}
