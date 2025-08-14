using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Text;

namespace Stripe.Endpoints.Webhooks.ProcessWebhook;

public static class ProcessWebhookRoute
{
    public const string Route = "";

    public static RouteGroupBuilder AddProcessWebhookRoute(this RouteGroupBuilder group)
    {
        group.MapPost(Route, async (
            HttpRequest request,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8);
            var payload = await reader.ReadToEndAsync(cancellationToken);
            
            var stripeSignature = request.Headers["Stripe-Signature"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(stripeSignature))
            {
                return Results.BadRequest("Missing Stripe-Signature header");
            }

            var command = new ProcessWebhookCommand(payload, stripeSignature);
            
            await mediator.Send(command, cancellationToken);
            
            return Results.Ok();
        })
        .AllowAnonymous();

        return group;
    }
}
