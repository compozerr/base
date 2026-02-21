namespace Cli.Endpoints.Modules.Search;

public sealed record SearchModulesResponse(
    SearchModuleItem[] Modules,
    int Total);

public sealed record SearchModuleItem(
    string Name,
    string Description,
    string LatestVersion,
    string RepoUrl,
    int InstallCount);
