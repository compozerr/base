using Api.Data.Repositories;
using Core.MediatR;

namespace Cli.Endpoints.Modules.Search;

public sealed class SearchModulesCommandHandler(
    IModuleRegistryRepository repository) :
    ICommandHandler<SearchModulesCommand, SearchModulesResponse>
{
    public async Task<SearchModulesResponse> Handle(
        SearchModulesCommand command,
        CancellationToken cancellationToken = default)
    {
        var (entries, totalCount) = await repository.SearchAsync(
            command.Query, command.Tags, command.Limit, command.Offset);

        var modules = entries.Select(e => new SearchModuleItem(
            e.Name,
            e.Description ?? "",
            e.LatestVersion ?? "0.0.0",
            e.RepoUrl.ToString(),
            e.InstallCount,
            e.Organization,
            e.Tags,
            e.IconUrl
        )).ToArray();

        return new SearchModulesResponse(modules, totalCount);
    }
}
