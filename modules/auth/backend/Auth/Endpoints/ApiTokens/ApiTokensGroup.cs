using Microsoft.AspNetCore.Builder;

namespace Auth.Endpoints.ApiTokens;

public class ApiTokensGroup : CarterModule
{
    public ApiTokensGroup() : base("/api-tokens")
    {
        WithTags("ApiTokens");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.AddCreateApiTokenRoute();
        app.AddListApiTokensRoute();
        app.AddRevokeApiTokenRoute();
    }
}
