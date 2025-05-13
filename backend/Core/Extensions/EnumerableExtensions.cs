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

    public static async Task ApplyAsync<T>(this IEnumerable<T>? items, Func<T, Task> action)
    {
        if (items == null)
            return;

        foreach (var item in items)
            await action(item);
    }

    public static async Task ApplyAsync<T>(this IEnumerable<T>? items, Func<T, CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        if (items == null)
            return;

        foreach (var item in items)
            await action(item, cancellationToken);
    }
}