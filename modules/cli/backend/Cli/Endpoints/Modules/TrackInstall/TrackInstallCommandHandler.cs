using Api.Data.Repositories;
using Core.MediatR;

namespace Cli.Endpoints.Modules.TrackInstall;

public sealed class TrackInstallCommandHandler(
    IModuleRegistryRepository repository) : ICommandHandler<TrackInstallCommand>
{
    public async Task Handle(TrackInstallCommand command, CancellationToken cancellationToken = default)
    {
        var entry = await repository.GetByOrgAndNameAsync(command.Organization, command.ModuleName);
        if (entry is not null)
            await repository.IncrementInstallCountAsync(entry.Id);
    }
}
