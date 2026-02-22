using Core.MediatR;
using Github.Repositories;

namespace Cli.Endpoints.Modules.SyncStatus;

public sealed class GetModuleSyncStatusCommandHandler(
    IModuleSyncEventRepository moduleSyncEventRepository)
    : ICommandHandler<GetModuleSyncStatusCommand, GetModuleSyncStatusResponse>
{
    public async Task<GetModuleSyncStatusResponse> Handle(
        GetModuleSyncStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var events = await moduleSyncEventRepository.GetFilteredAsync(
            x => x.ProjectId == command.ProjectId);

        // Return the most recent sync per module
        var latestByModule = events
            .GroupBy(e => e.ModuleName)
            .Select(g => g.OrderByDescending(e => e.CreatedAtUtc).First())
            .Select(e => new ModuleSyncStatusItem(
                e.ModuleName,
                e.Status,
                e.SourceCommitHash,
                e.TargetCommitHash,
                e.FilesChanged,
                e.CompletedAt,
                e.ErrorMessage))
            .ToList();

        return new GetModuleSyncStatusResponse(latestByModule);
    }
}
