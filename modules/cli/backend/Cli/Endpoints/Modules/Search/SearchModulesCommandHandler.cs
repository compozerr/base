using Core.MediatR;

namespace Cli.Endpoints.Modules.Search;

public sealed class SearchModulesCommandHandler :
    ICommandHandler<SearchModulesCommand, SearchModulesResponse>
{
    public Task<SearchModulesResponse> Handle(
        SearchModulesCommand command,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement full-text search when marketplace data exists
        // For now return empty results
        return Task.FromResult(new SearchModulesResponse(
            Modules: Array.Empty<SearchModuleItem>(),
            Total: 0));
    }
}
