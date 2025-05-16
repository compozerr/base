namespace Cli.Endpoints.Modules.Add;

public static class ModuleResultExtensions
{
    public static ModuleResult[] WithIsOwner(this ModuleResult[] moduleResults, string organization)
        => [.. moduleResults.Select(m => m with { IsOwner = m.Module?.OwnsRepo(organization) })];
}