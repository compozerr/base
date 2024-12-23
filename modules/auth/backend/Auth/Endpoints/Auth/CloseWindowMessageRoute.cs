using Microsoft.AspNetCore.Builder;

namespace Auth.Endpoints.Auth;

public static class CloseWindowMessageRoute
{
    public const string Route = "close-window-message";

    public static RouteHandlerBuilder AddCloseWindowMessageRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet(Route, () => "You can now close this window...");
    }
}
