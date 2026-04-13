using Api.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.Config;

public sealed record ReportProjectConfigRequest(
    string Name);

public static class ReportProjectConfigRoute
{
    public const string Route = "{projectId:guid}/config";

    public static RouteHandlerBuilder AddReportProjectConfigRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPut(Route, ExecuteAsync);
    }

    public static Task<ReportProjectConfigResponse> ExecuteAsync(
        ProjectId projectId,
        ReportProjectConfigRequest request,
        IMediator mediator)
        => mediator.Send(
            new ReportProjectConfigCommand(
                projectId,
                request.Name));
}
