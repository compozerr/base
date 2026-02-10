using Auth.Services;
using Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Auth.Endpoints.ApiTokens;

public static class RevokeApiTokenRoute
{
    public static RouteHandlerBuilder AddRevokeApiTokenRoute(this IEndpointRouteBuilder app)
    {
        return app.MapDelete("/{id:guid}", async (
            Guid id,
            ICurrentUserAccessor currentUserAccessor,
            IApiTokenService apiTokenService) =>
        {
            var userId = currentUserAccessor.CurrentUserId;
            userId.ThrowIfNull(nameof(userId));

            await apiTokenService.RevokeTokenAsync(ApiTokenId.Create(id), userId);

            return TypedResults.NoContent();
        });
    }
}
