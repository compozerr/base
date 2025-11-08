using Api.Features.N8n.Endpoints.CreateN8nProject;

namespace Api.Features.N8n.Endpoints;

public class N8nGroup : CarterModule
{
    public N8nGroup() : base("/n8n")
    {
        WithTags(nameof(N8nGroup));
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddCreateN8nProjectRoute();
    }
}
