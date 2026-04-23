using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Api.Hosting.Endpoints.Projects.ProductionEnvironment;

public static class ProductionEnvironmentGroup
{
    public const string Route = "{projectId:guid}/environments/production";

    public static RouteGroupBuilder AddProductionEnvironmentGroup(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route);

        group.AddGetProductionEnvironmentRoute();

        return group;
    }
}
