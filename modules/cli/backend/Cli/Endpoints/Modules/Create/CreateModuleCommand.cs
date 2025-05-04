using Core.MediatR;

namespace Cli.Endpoints.Modules.Create;

public sealed record CreateModuleCommand(
    string RepoName,
    string RepoUrl) : ICommand<CreateModuleResponse>;
