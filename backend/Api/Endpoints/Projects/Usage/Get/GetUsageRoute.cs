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

    public static Task<GetUsageResponse> ExecuteAsync(Guid projectId, string usageSpan, IMediator mediator)
    {
        if (!Enum.TryParse<UsageSpan>(usageSpan, out var usageSpanConverted))
        {
            throw new ArgumentException("Invalid usage span");
        }

        return mediator.Send(
            new GetUsageCommand(
                ProjectId.Create(projectId),
                usageSpanConverted));
    }
}
