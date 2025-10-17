using Api.Abstractions;
using Api.Data;
using Api.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.ProjectDomains;

public sealed record DomainDto(
    string ServiceName,
    string Port,
    string Protocol,
    string Value);

public static class GetDomainsRoute
{
    public const string Route = "/";

    public static RouteHandlerBuilder AddGetDomainsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static async Task<Results<Ok<List<DomainDto>>, NoContent>> ExecuteAsync(
        ProjectId projectId,
        IProjectRepository projectRepository)
    {
        var project = await projectRepository.GetProjectByIdWithDomainsAsync(projectId);

        if (project is null)
        {
            return TypedResults.NoContent();
        }

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
                        domain.Protocol,
                        value));
            }
        }

        return TypedResults.Ok(domainDtos);
    }
}
