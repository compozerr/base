using Auth.Services;
using Github.Endpoints.SetDefaultInstallationId;
using Github.Services;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Github.Endpoints;

public sealed record SetDefaultOrganizationRequest(string InstallationId);

public static class SetDefaultOrganizationRoute
{
    public const string Route = "set-deafult-organization";

    public static RouteHandlerBuilder AddSetDefaultOrganizationRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost(Route, ExecuteAsync);
    }

    public static async Task<Results<Ok, NotFound>> ExecuteAsync(
        SetDefaultOrganizationRequest request,
        ICurrentUserAccessor currentUserAccessor,
        IGithubService githubService,
        IMediator mediator)
    {
        var userId = currentUserAccessor.CurrentUserId!;

        var installations = await githubService.GetInstallationsForUserAsync(userId);

        if (!installations.Select(i => i.InstallationId).Contains(request.InstallationId))
        {
            return TypedResults.NotFound();
        }

        await mediator.Send(new SetDefaultInstallationIdCommand(userId, request.InstallationId));

        return TypedResults.Ok();
    }
}