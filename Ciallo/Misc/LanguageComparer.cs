using System;
using System.Globalization;

namespace Ciallo.Misc;

// Gen by copilot
public sealed class LanguageComparer : StringComparer
{
    /// <summary>
    /// Normalizes a culture string by taking everything before the first underscore (or the whole string if there is none).
    /// </summary>
    private static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;
        
        var parts = s.Split(['_', '-']);
        return parts[0];
    }

    /// <summary>
    /// Compares two culture strings by their normalized "language" portion, ignoring case.
    /// </summary>
    public override int Compare(string x, string y)
    {
        var nx = Normalize(x);
        var ny = Normalize(y);
        return StringComparer.OrdinalIgnoreCase.Compare(nx, ny);
    }

    /// <summary>
    /// Returns true if the normalized language portions of x and y are equal.
    /// </summary>
    public override bool Equals(string x, string y)
    {
        var nx = Normalize(x);
        var ny = Normalize(y);
        return StringComparer.OrdinalIgnoreCase.Equals(nx, ny);
    }

    /// <summary>
    /// Computes a hash code based on the normalized language portion of the culture string.
    /// </summary>
    public override int GetHashCode(string obj)
    {
        var n = Normalize(obj);
        return StringComparer.OrdinalIgnoreCase.GetHashCode(n);
    }

    /// <summary>
    /// A ready‐to‐use instance of the comparer.
    /// </summary>
    public static readonly StringComparer Instance = new LanguageComparer();
}