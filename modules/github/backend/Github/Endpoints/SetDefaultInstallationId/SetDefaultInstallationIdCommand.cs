using Core.MediatR;
using Organizations.Abstractions;

namespace Github.Endpoints.SetDefaultInstallationId;

public enum DefaultInstallationIdSelectionType
{
    Projects = 1,
    Modules = 2
}

public sealed record SetDefaultInstallationIdCommand(
    OrganizationId OrganizationId,
    string InstallationId,
    DefaultInstallationIdSelectionType DefaultInstallationIdSelectionType
) : ICommand<string>;
