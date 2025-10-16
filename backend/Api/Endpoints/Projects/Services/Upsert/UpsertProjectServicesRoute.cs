using Api.Abstractions;
using MediatR;

namespace Api.Endpoints.Projects.Services.Upsert;

public static class UpsertProjectServicesRoute
{
    public const string Route = "services";

    public static RouteHandlerBuilder AddUpsertProjectServicesRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPut(Route, ExecuteAsync);
    }

    public static Task<UpsertProjectServicesResponse> ExecuteAsync(
        ProjectId projectId,
        UpsertProjectServicesRequest request,
        IMediator mediator)
        => mediator.Send(
            new UpsertProjectServicesCommand(
                projectId,
                request.Services));
}
