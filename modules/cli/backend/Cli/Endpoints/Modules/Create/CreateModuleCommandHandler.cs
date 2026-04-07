using Api.Data;
using Api.Data.Repositories;
using Core.MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cli.Endpoints.Modules.Create;

public sealed class CreateModuleCommandHandler(
    IModuleRepository moduleRepository,
    ApiDbContext db) : ICommandHandler<CreateModuleCommand, CreateModuleResponse>
{
    public async Task<CreateModuleResponse> Handle(CreateModuleCommand command, CancellationToken cancellationToken = default)
    {
        // Idempotent: skip if a module with this name already exists
        var existing = await db.Modules.FirstOrDefaultAsync(
            m => m.Name == command.RepoName, cancellationToken);
        if (existing != null)
        {
            return new CreateModuleResponse();
        }

        await moduleRepository.AddAsync(new Module()
        {
            Name = command.RepoName,
            RepoUri = new Uri(command.RepoUrl)
        }, cancellationToken);

        return new CreateModuleResponse();
    }
}
