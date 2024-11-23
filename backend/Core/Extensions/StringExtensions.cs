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
}