using Api.Data;
using Api.Data.Repositories;
using Auth.Services;
using Core.MediatR;

namespace Cli.Endpoints.Modules.Release;

public sealed class ReleaseModuleCommandHandler(
    ApiDbContext apiDb,
    ICurrentUserAccessor currentUserAccessor,
    IModuleRegistryRepository registryRepository) :
    ICommandHandler<ReleaseModuleCommand, ReleaseModuleResponse>
{
    public async Task<ReleaseModuleResponse> Handle(
        ReleaseModuleCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserAccessor.CurrentUserId
            ?? throw new InvalidOperationException("User not authenticated");

        try
        {
            var entry = await registryRepository.GetByOrgAndNameAsync(
                command.Organization, command.ModuleName);

            if (entry is null)
            {
                entry = new ModuleRegistryEntry
                {
                    Name = command.ModuleName,
                    Organization = command.Organization,
                    Description = command.Description,
                    RepoUrl = new Uri(command.RepoUrl ?? $"https://github.com/{command.Organization}/{command.ModuleName}"),
                    LatestVersion = command.Version,
                    Tags = command.Tags ?? [],
                    Compatibility = command.Compatibility,
                    PublishedByUserId = userId,
                    CreatedAtUtc = DateTime.UtcNow,
                };
                apiDb.ModuleRegistryEntries.Add(entry);
            }
            else
            {
                entry.LatestVersion = command.Version;
                entry.UpdatedAtUtc = DateTime.UtcNow;
                if (command.Description is not null) entry.Description = command.Description;
                if (command.Tags is not null) entry.Tags = command.Tags;
                if (command.Compatibility is not null) entry.Compatibility = command.Compatibility;
                if (command.RepoUrl is not null) entry.RepoUrl = new Uri(command.RepoUrl);
            }

            var versionEntry = new ModuleVersionEntry
            {
                ModuleRegistryEntryId = entry.Id,
                Version = command.Version,
                CommitHash = command.CommitHash ?? "unknown",
                ReleaseNotes = command.ReleaseNotes,
                CreatedAtUtc = DateTime.UtcNow,
            };
            apiDb.ModuleVersionEntries.Add(versionEntry);

            await apiDb.SaveChangesAsync(cancellationToken);

            return new ReleaseModuleResponse(true, $"v{command.Version}", null);
        }
        catch (Exception ex)
        {
            return new ReleaseModuleResponse(false, null, ex.Message);
        }
    }
}
