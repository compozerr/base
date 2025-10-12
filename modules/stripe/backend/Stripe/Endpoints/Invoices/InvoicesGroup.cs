using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Stripe.Endpoints.Invoices.GetInvoices;

namespace Stripe.Endpoints.Invoices;

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
