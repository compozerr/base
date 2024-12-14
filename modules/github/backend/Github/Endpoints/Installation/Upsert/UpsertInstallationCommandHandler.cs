using Core.MediatR;
using Github.Abstractions;
using Github.Repositories;

namespace Github.Endpoints.Installation.Upsert;

public sealed class UpsertInstallationCommandHandler(IInstallationRepository installationRepository) : ICommandHandler<UpsertInstallationCommand, InstallationId>
{
    public Task<InstallationId> Handle(UpsertInstallationCommand command, CancellationToken cancellationToken = default)
     => installationRepository.AddInstallationAsync(command.UserId, command.AccessToken, command.Scope, cancellationToken);
}
