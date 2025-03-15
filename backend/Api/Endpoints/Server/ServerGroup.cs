
namespace Api.Endpoints.Server;

public class ServerGroup : CarterModule
{
    public ServerGroup() : base("/servers")
    {
        WithTags(nameof(Server));
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddCreateNewServerRoute();
        app.AddUpdateServerRoute();
    }
}
