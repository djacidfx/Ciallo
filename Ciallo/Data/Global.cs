global using static Ciallo.Data.Global;
using ObservableCollections;

namespace Ciallo.Data;

public static partial class Global
{
    public static readonly Preference AppPreference = new();
    public static readonly ObservableList<BrushSetting> AppBrushes = [];
}