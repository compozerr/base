using Auth.Abstractions;
using Core.MediatR;

namespace Github.Endpoints.SetDefaultInstallationId;

public enum DefaultInstallationIdSelectionType
{
    Projects = 1,
    Modules = 2
}

public record SetDefaultInstallationIdCommand(
    UserId UserId,
    string InstallationId,
    DefaultInstallationIdSelectionType DefaultInstallationIdSelectionType) : ICommand;