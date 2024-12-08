
namespace Api.Endpoints.Home;

public class HomeGroup : CarterModule
{
    public HomeGroup() : base("/")
    {
        WithTags(nameof(Home));
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddHomeRoute();
    }
}
