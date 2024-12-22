using Core.MediatR;
using Github.Repositories;

namespace Github.Endpoints.SetDefaultInstallationId;

public class SetDefaultInstallationIdCommandHandler(
    IGithubUserSettingsRepository githubUserSettingsRepository) : ICommandHandler<SetDefaultInstallationIdCommand>
{
    public Task Handle(SetDefaultInstallationIdCommand command, CancellationToken cancellationToken = default)
        => githubUserSettingsRepository.SetSelectedOrganizationForUserAsync(
            command.UserId,
            command.InstallationId);
}
