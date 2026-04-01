using Godot;

namespace Ciallo;

public static class StringExtension
{
    public static string Tr(this string s) => TranslationServer.Translate(s);
}