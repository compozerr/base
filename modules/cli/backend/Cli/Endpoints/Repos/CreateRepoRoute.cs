using Github.Endpoints.SetDefaultInstallationId;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Cli.Endpoints.Repos;

public record CreateRepoRequest(string Name, DefaultInstallationIdSelectionType Type);

public static class CreateRepoRoute
{
    public const string Route = "/";

    public static RouteHandlerBuilder AddCreateRepoRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static Task<CreateRepoResponse> ExecuteAsync(
        CreateRepoRequest createRepoRequest,
        IMediator mediator)
        => mediator.Send(
            new CreateRepoCommand(
                createRepoRequest.Name,
                createRepoRequest.Type));
}
