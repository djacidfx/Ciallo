using System;
using R3;

namespace Ciallo.Misc;

public static class UndoReactivePropertyExtension
{
    public static void SetUndoable<T>(this ReactiveProperty<T> property, PropertyUndoOption options = default)
    {
        var currentValue = property.CurrentValue;
    }

    public struct PropertyUndoOption
    {
        public string ActionName = "Set Property";
        public TimeSpan UndoGroupingInterval = TimeSpan.FromMilliseconds(100);

        public PropertyUndoOption()
        {
        }
    }
}