using System.Diagnostics.CodeAnalysis;

namespace Core.Extensions;

public static class StringExtensions
{
    public static T ToEnum<T>(this string value) where T : struct
    {
        return (T)Enum.Parse(typeof(T), value, true);
    }

    public static string RemoveWhitespace(this string value)
    {
        return string.Join(" ", value.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Indicates whether a specified string is <c>null</c>, empty, or consists only of white-space characters.
    /// </summary>
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? str)
        => string.IsNullOrWhiteSpace(str);

    /// <summary>
    /// Returns a trimmed version of <paramref name="str" /> or null if <paramref name="str" /> is null or whitespace.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string? Clean(this string? str)
        => !str.IsNullOrWhiteSpace()
            ? str.Trim()
            : null;

}