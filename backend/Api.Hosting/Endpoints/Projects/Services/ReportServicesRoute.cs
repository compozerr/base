using Api.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.Services;

public static class ReportServicesRoute
{
    public const string Route = "{projectId:guid}/services";

    public static RouteHandlerBuilder AddReportServicesRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<ReportServicesResponse> ExecuteAsync(
        ProjectId projectId,
        ReportServicesRequest request,
        IMediator mediator)
        => mediator.Send(
            new ReportServicesCommand(
                projectId,
                request.Services));
}
