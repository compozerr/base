using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Usage.Get;

public static class GetUsageRoute
{
    public const string Route = "{projectId:guid}/{usageSpan}";

    public static RouteHandlerBuilder AddGetUsageRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, ExecuteAsync);
    }

    public static Task<GetUsageResponse> ExecuteAsync(
        ProjectId projectId,
        UsageSpan usageSpan,
        IMediator mediator)
    {
        return mediator.Send(
            new GetUsageCommand(
                projectId,
                usageSpan));
    }
}
