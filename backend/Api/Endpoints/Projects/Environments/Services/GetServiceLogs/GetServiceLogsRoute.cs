using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Environments.Services.GetServiceLogs;

public static class GetServiceLogsRoute
{
    public const string Route = "{environmentId:guid}/services/{serviceName}/logs";

    public static RouteHandlerBuilder AddGetServiceLogsRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    private static Task<GetServiceLogsResponse> ExecuteAsync(
        ProjectId projectId,
        ProjectEnvironmentId environmentId,
        string serviceName,
        int? tail,
        IMediator mediator)
        => mediator.Send(new GetServiceLogsCommand(projectId, environmentId, serviceName, tail ?? 200));
}
