using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Organizations.Abstractions;

namespace Organizations.Endpoints.Update;

public static class UpdateOrganizationRoute
{
    public const string Route = "{organizationId:guid}";

    public static RouteHandlerBuilder AddUpdateOrganizationRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPut(Route, async (
            OrganizationId organizationId,
            UpdateOrganizationRequest request,
            IMediator mediator) =>
        {
            var result = await mediator.Send(new UpdateOrganizationCommand(organizationId, request.Name, request.AvatarUrl));
            return TypedResults.Ok(result);
        });
    }
}

public sealed record UpdateOrganizationRequest(string Name, string? AvatarUrl);
