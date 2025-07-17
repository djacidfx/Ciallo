using System;
using Godot;
using R3;
using Humanizer;

namespace Ciallo.Misc;

/// <summary>
/// The control to display an enum in the editor.
/// </summary>
public class PropertyEnumUI : IPropertyUI
{
    private readonly OptionButton _optionButton;
    public string Name { get; }

    public Control Control => _optionButton;

    public PropertyEnumUI(string name)
    {
        Name = name;
        _optionButton = new();
    }

    public void Bind<T>(ReactiveProperty<T> property) where T : Enum
    {
        _optionButton.Clear();
        var memberNames = Enum.GetNames(typeof(T));
        var values = Enum.GetValues(typeof(T));

        foreach (var name in memberNames)
        {
            _optionButton.AddItem(name.Humanize());
        }
        
        // Set current value
        _optionButton.Selected = Array.IndexOf(values, property.Value);
        // Bind
        _optionButton.ItemSelected += index =>
        {
            if (index < 0 || index >= values.Length)
                return;
            property.Value = (T)values.GetValue(index)!;
        };
        property.Subscribe(value =>
        {
            _optionButton.Selected = Array.IndexOf(values, value);
        }).AddTo(_optionButton);
    }
}