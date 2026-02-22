using Api.Data.Repositories;
using Core.MediatR;

namespace Cli.Endpoints.Modules.Info;

public sealed class GetModuleInfoCommandHandler(
    IModuleRegistryRepository repository) :
    ICommandHandler<GetModuleInfoCommand, GetModuleInfoResponse>
{
    public async Task<GetModuleInfoResponse> Handle(
        GetModuleInfoCommand command,
        CancellationToken cancellationToken = default)
    {
        var entry = await repository.GetByOrgAndNameAsync(command.Organization, command.ModuleName);

        if (entry is null)
        {
            return new GetModuleInfoResponse(
                Found: false, null, null, null, null, null, null, null, 0, null, null, null);
        }

        var versions = entry.Versions.Select(v => new ModuleVersionDto(
            v.Version, v.CommitHash, v.ReleaseNotes, v.CreatedAtUtc
        )).ToArray();

        return new GetModuleInfoResponse(
            Found: true,
            entry.Name,
            entry.Organization,
            entry.Description,
            entry.LatestVersion,
            entry.RepoUrl.ToString(),
            entry.Tags,
            versions,
            entry.InstallCount,
            entry.IconUrl,
            entry.Compatibility,
            entry.CreatedAtUtc);
    }
}
