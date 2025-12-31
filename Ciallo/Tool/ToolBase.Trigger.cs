using System.Collections.Generic;
using Ciallo.Command;
using Godot;

namespace Ciallo.Tool;

public abstract partial class ToolBase
{
    public class Trigger(string Name)
    {
        public static readonly Trigger Activate = new("Activate");
        public static readonly Trigger Deactivate = new("Deactivate");
        public static readonly Trigger Refresh = new("Refresh");

        private static readonly Dictionary<MouseButton, Trigger> MouseButtonPress = new();
        private static readonly Dictionary<MouseButton, Trigger> MouseButtonRelease = new();
        private static readonly Dictionary<Key, Trigger> KeyPress = new();
        private static readonly Dictionary<Key, Trigger> KeyRelease = new();
        private static readonly Dictionary<AppAction, Trigger> AppActionPress = new();
        private static readonly Dictionary<AppAction, Trigger> AppActionRelease = new();

        public static Trigger Get(MouseButton button, bool isPress)
        {
            var dict = isPress ? MouseButtonPress : MouseButtonRelease;
            if (!dict.TryGetValue(button, out var trigger))
            {
                var action = isPress ? "Press" : "Release";
                trigger = new Trigger($"{action}({button})");
                dict[button] = trigger;
            }
            return trigger;
        }

        public static Trigger Get(Key key, bool isPress)
        {
            var dict = isPress ? KeyPress : KeyRelease;
            if (!dict.TryGetValue(key, out var trigger))
            {
                var action = isPress ? "Press" : "Release";
                trigger = new Trigger($"{action}({key})");
                dict[key] = trigger;
            }
            return trigger;
        }

        public static Trigger Get(AppAction action, bool isPress)
        {
            var dict = isPress ? AppActionPress : AppActionRelease;
            if (!dict.TryGetValue(action, out var trigger))
            {
                var actionStr = isPress ? "Press" : "Release";
                trigger = new Trigger($"{actionStr}({action.Name})");
                dict[action] = trigger;
            }
            return trigger;
        }
    }
}