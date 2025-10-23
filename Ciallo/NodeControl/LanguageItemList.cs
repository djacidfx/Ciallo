using System.Collections.Generic;
using System.Globalization;
using Ciallo.Data;
using Ciallo.Misc;
using Godot;

namespace Ciallo.NodeControl;

[Tool]
public partial class LanguageItemList : ItemList
{
    public override void _Ready()
    {
        Clear();

        if (Engine.IsEditorHint())
        {
            foreach (var langCode in Preference.SupportedLanguages)
            {
                AddItem(ToNativeName(langCode));
            }
        }
        else this.BindValue(Preference.SupportedLanguages, AppPreference.Language, ToNativeName);

        return;

        string ToNativeName(string langCode)
        {
            Dictionary<string, string> codeConvert = new()
            {
                { "zh_CN", "zh-Hans" },
                { "zh_TW", "zh-Hant" }
            };
            langCode = codeConvert.GetValueOrDefault(langCode, langCode);
            return CultureInfo.GetCultureInfo(langCode).NativeName;
        }
    }
}