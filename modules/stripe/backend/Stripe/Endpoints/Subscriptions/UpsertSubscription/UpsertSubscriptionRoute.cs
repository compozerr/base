using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Core.MediatR;
using MediatR;
using Api.Abstractions;

namespace Stripe.Endpoints.Subscriptions.UpsertSubscription;

public static class UpsertSubscriptionRouteExtension
{
    public static RouteGroupBuilder AddUpsertSubscriptionRoute(this RouteGroupBuilder group)
    {
        group.MapPost("upsert", async (
            UpsertSubscriptionRequest request,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new UpsertSubscriptionCommand(
                ProjectId: ProjectId.Create(request.ProjectId),
                ServerTierId: new ServerTierId(request.Tier));

            var response = await sender.Send(command, cancellationToken);
            return Results.Ok(response);
        })
        .RequireAuthorization();

        return group;
    }
}

public class UpsertSubscriptionRequest
{
    public Guid ProjectId { get; set; }
    public string Tier { get; set; } = string.Empty;
}
