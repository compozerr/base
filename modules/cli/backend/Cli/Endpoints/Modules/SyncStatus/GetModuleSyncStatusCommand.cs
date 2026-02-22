using Api.Abstractions;
using Core.MediatR;

namespace Cli.Endpoints.Modules.SyncStatus;

public sealed record GetModuleSyncStatusCommand(
    ProjectId ProjectId) : ICommand<GetModuleSyncStatusResponse>;
