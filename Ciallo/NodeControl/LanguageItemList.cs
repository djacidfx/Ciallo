using Godot;
using System;
using System.Collections.Generic;
using Ciallo.Data;
using System.Globalization;
using Ciallo.Misc;

namespace Ciallo.NodeControl;

[Tool]
public partial class LanguageItemList : ItemList
{
    public override void _Ready()
    {
        Clear();

        if (Engine.IsEditorHint())
        {
            foreach(var langCode in Preferences.SupportedLanguages)
            {
                AddItem(ToNativeName(langCode));
            }
        }
        else this.BindValue(Preferences.SupportedLanguages, Preferences.Language, ToNativeName);

        return;

        string ToNativeName(string langCode)
        {
            Dictionary<string, string> codeConvert = new()
            {
                {"zh_CN", "zh-Hans"},
                {"zh_TW", "zh-Hant"}
            };
            langCode = codeConvert.GetValueOrDefault(langCode, langCode);
            return CultureInfo.GetCultureInfo(langCode).NativeName;
        }
    }
}
