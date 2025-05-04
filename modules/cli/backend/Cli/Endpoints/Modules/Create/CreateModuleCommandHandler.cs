using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;

namespace Cli.Endpoints.Modules.Create;

public sealed class CreateModuleCommandHandler(
    IModuleRepository moduleRepository) : ICommandHandler<CreateModuleCommand, CreateModuleResponse>
{
    public async Task<CreateModuleResponse> Handle(CreateModuleCommand command, CancellationToken cancellationToken = default)
    {
        await moduleRepository.AddAsync(new Module()
        {
            Name = command.RepoName,
            RepoUri = new Uri(command.RepoUrl)
        }, cancellationToken);

        return new CreateModuleResponse();
    }
}
