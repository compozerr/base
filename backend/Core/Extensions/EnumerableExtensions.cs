namespace Core.Extensions;

public static class EnumerableExtensions
{
    public static void Apply<T>(this IEnumerable<T>? items, Action<T> action)
    {
        if (items == null)
            return;

        foreach (var item in items)
            action(item);
    }
}