using Core.MediatR;

namespace Cli.Endpoints.Modules.Search;

public sealed record SearchModulesCommand(
    string? Query,
    string[]? Tags = null,
    int Limit = 20,
    int Offset = 0) : ICommand<SearchModulesResponse>;
