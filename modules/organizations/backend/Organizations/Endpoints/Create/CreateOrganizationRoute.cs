using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Organizations.Endpoints.Create;

public static class CreateOrganizationRoute
{
    public static RouteHandlerBuilder AddCreateOrganizationRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost("", async (CreateOrganizationCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return TypedResults.Created($"/organizations/{result.Id}", result);
        });
    }
}
