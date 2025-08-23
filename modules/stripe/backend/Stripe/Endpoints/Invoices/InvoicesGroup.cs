using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Stripe.Endpoints.PaymentMethods.GetInvoices;


namespace Stripe.Endpoints.PaymentMethods;

public static class InvoicesGroup
{
    public const string Route = "invoices";

    public static RouteGroupBuilder AddInvoicesGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetInvoicesRoute();

        return group;
    }
}
