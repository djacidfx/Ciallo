using Godot;

namespace Ciallo.Misc;

public static class StringExtension
{
    public static string Tr(this string s) => TranslationServer.Translate(s);
}