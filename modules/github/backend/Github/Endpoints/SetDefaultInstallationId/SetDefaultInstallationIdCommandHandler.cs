using Core.MediatR;
using Github.Repositories;

namespace Github.Endpoints.SetDefaultInstallationId;

public class SetDefaultInstallationIdCommandHandler(
    IGithubOrganizationSettingsRepository githubOrganizationSettingsRepository) : ICommandHandler<SetDefaultInstallationIdCommand, string>
{
    public Task<string> Handle(SetDefaultInstallationIdCommand command, CancellationToken cancellationToken = default)
        => githubOrganizationSettingsRepository.SetSelectedOrganizationForOrgAsync(
            command.OrganizationId,
            command.InstallationId,
            command.DefaultInstallationIdSelectionType);
}
