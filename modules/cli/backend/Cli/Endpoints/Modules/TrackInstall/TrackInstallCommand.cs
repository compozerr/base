using Core.MediatR;

namespace Cli.Endpoints.Modules.TrackInstall;

public sealed record TrackInstallCommand(
    string Organization,
    string ModuleName) : ICommand;
