using MediatR;

namespace Api.Features.N8n.Endpoints.CreateN8nProject;

public record CreateN8nProjectRequest(
    string ProjectName,
    string LocationIso,
    string Tier);

public static class CreateN8nProjectRoute
{
    public const string Route = "/projects";

    public static RouteHandlerBuilder AddCreateN8nProjectRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<CreateN8nProjectResponse> ExecuteAsync(
        CreateN8nProjectRequest request,
        IMediator mediator)
        => mediator.Send(
            new CreateN8nProjectCommand(
                request.ProjectName,
                request.LocationIso,
                request.Tier));
}
