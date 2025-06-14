namespace Api.Abstractions;

public static class ServerTiers
{
    private static readonly ServerTier T0 = new(new("T0"), 0.5m, 0.5m, 50, new(5, "USD"), null);
    private static readonly ServerTier T1 = new(new("T1"), 1, 1, 50, new(8, "USD"), null);
    private static readonly ServerTier T2 = new(new("T2"), 2, 2, 50, new(14, "USD"), null);
    private static readonly ServerTier T3 = new(new("T3"), 4, 2, 50, new(25, "USD"), null);
    private static readonly ServerTier T4 = new(new("T4"), 8, 3, 50, new(40, "USD"), null);

    public static readonly List<ServerTier> All =
    [
        T0,
        T1,
        T2,
        T3,
        T4
    ];

    public static ServerTier GetById(ServerTierId id)
    {
        return All.FirstOrDefault(t => t.Id == id)
               ?? throw new ArgumentException($"Server tier with ID {id.Value} not found.");
    }
}