using Core.MediatR;
using Github.Endpoints.SetDefaultInstallationId;

namespace Cli.Endpoints.Repos;

public sealed record CreateRepoCommand(
    string Name,
    DefaultInstallationIdSelectionType Type) : ICommand<CreateRepoResponse>;