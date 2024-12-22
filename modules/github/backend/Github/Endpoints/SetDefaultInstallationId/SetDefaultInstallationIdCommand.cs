using Auth.Abstractions;
using Core.MediatR;

namespace Github.Endpoints.SetDefaultInstallationId;

public record SetDefaultInstallationIdCommand(UserId UserId, string InstallationId) : ICommand;