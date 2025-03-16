using Api.Abstractions;
using Api.Data;
using Api.Hosting.Services;
using Core.Abstractions;

namespace Cli.Endpoints.Projects;

public sealed class AllocateDomains_ProjectCreatedEventHandler : IDomainEventHandler<ProjectCreatedEvent>
{
    public Task Handle(ProjectCreatedEvent notification, CancellationToken cancellationToken)
    {
        notification.Entity.Domains = [.. GetDomains(notification.Entity)];

        return Task.CompletedTask;
    }

    private static List<InternalDomain> GetDomains(Project project)
    {
        var hash = GetHash(RepoUri.Parse(project.RepoUri).RepoName);

        return [GetFrontendDomain(hash, project.Id), GetBackendDomain(hash, project.Id)];
    }

    private static string GetHash(string repoName)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(repoName);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLower()[..8];
    }

    private static InternalDomain GetFrontendDomain(string hash, ProjectId projectId)
        => new()
        {
            ProjectId = projectId,
            ServiceName = "Frontend",
            Port = "1234",
            Subdomain = $"{hash}",
        };

    private static InternalDomain GetBackendDomain(string hash, ProjectId projectId)
        => new()
        {
            ProjectId = projectId,
            ServiceName = "Backend",
            Port = "1235",
            Subdomain = $"api.{hash}",
        };
}