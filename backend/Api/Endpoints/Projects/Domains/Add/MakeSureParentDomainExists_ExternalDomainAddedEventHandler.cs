using Api.Data;
using Api.Data.Repositories;
using Api.Hosting.Services;
using Core.Abstractions;

namespace Api.Endpoints.Projects.Domains.Add;

public sealed class MakeSureParentDomainExists_ExternalDomainAddedEventHandler(
    IDomainRepository domainRepository,
    IProjectRepository projectRepository,
    IProjectServiceRepository projectServiceRepository) : IDomainEventHandler<ExternalDomainAddedEvent>
{
    private readonly Serilog.ILogger _logger = Log.Logger.ForContext<MakeSureParentDomainExists_ExternalDomainAddedEventHandler>();
    public async Task Handle(ExternalDomainAddedEvent notification, CancellationToken cancellationToken)
    {
        var currentDomains = await domainRepository.GetAllAsync(
           x => x.Where(x => x.ProjectId == notification.Entity.ProjectId),
           cancellationToken);

        if (
            !currentDomains.Any(
                x => x.Port == notification.Entity.Port && x.Protocol == notification.Entity.Protocol))
        {
            _logger.Information("No parent domain found for project {ProjectId} on port {Port} and protocol {Protocol}. Creating one.",
                notification.Entity.ProjectId,
                notification.Entity.Port,
                notification.Entity.Protocol);

            var projectService = await projectServiceRepository.GetSingleAsync(
                x => x.Port == notification.Entity.Port && x.Protocol == notification.Entity.Protocol && x.ProjectId == notification.Entity.ProjectId,
                cancellationToken) ?? throw new InvalidOperationException("Project service not found");

            var project = await projectRepository.GetByIdAsync(
                notification.Entity.ProjectId,
                cancellationToken) ?? throw new InvalidOperationException("Project not found");

            var parentDomain = BuildCustomInternalDomain(
                project,
                projectService);

            await domainRepository.AddAsync(
                parentDomain,
                cancellationToken);
        }
        else
        {
            _logger.Information("Parent domain already exists for project {ProjectId} on port {Port} and protocol {Protocol}. No action needed.",
                notification.Entity.ProjectId,
                notification.Entity.Port,
                notification.Entity.Protocol);
        }
    }

    private static string GetHash(string repoName)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(repoName);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLower()[..8];
    }

    private static InternalDomain BuildCustomInternalDomain(Data.Project project, ProjectService service)
    {
        var hash = GetHash(RepoUri.Parse(project.RepoUri).RepoName);

        return new()
        {
            ProjectId = project.Id,
            ServiceName = service.Name,
            Port = service.Port,
            Protocol = service.Protocol,
            IsPrimary = false,
            Subdomain = $"{service.Name.ToLower()}.{hash}",
        };
    }
}
