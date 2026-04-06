using Api.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.Services;

public static class ReportEnvironmentServicesRoute
{
    public const string Route = "{projectId:guid}/environments/{environmentId:guid}/services";

    public static RouteHandlerBuilder AddReportEnvironmentServicesRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<ReportServicesResponse> ExecuteAsync(
        ProjectId projectId,
        ProjectEnvironmentId environmentId,
        ReportServicesRequest request,
        IMediator mediator)
        => mediator.Send(
            new ReportEnvironmentServicesCommand(
                projectId,
                environmentId,
                request.Services));
}
