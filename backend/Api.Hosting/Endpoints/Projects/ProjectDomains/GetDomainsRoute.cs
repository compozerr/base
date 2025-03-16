using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.ProjectDomains;

public sealed record DomainDto(
    string ServiceName,
    string Port,
    string Value);

public static class GetDomainsRoute
{
    public const string Route = "/";

    public static RouteHandlerBuilder AddGetDomainsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<List<DomainDto>> ExecuteAsync(Guid projectId, IProjectRepository projectRepository)
    {
        var convertedProjectId = ProjectId.Create(projectId);

        var project = await projectRepository.GetProjectByIdWithDomainsAsync(convertedProjectId) ?? throw new ArgumentException("Project not found");

        var domainDtos = new List<DomainDto>();

        foreach (var domain in project.Domains!)
        {
            var value = domain switch
            {
                ExternalDomain externalDomain => externalDomain.Value,
                InternalDomain internalDomain => internalDomain.Value,
                _ => string.Empty,
            };

            if (!string.IsNullOrEmpty(value))
            {
                domainDtos.Add(
                    new DomainDto(
                        domain.ServiceName,
                        domain.Port,
                        value));
            }
        }

        return domainDtos;
    }
}
