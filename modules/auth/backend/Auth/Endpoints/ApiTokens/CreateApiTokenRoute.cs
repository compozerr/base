using Auth.Services;
using Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Auth.Endpoints.ApiTokens;

public record CreateApiTokenRequest(string Name, DateTime? ExpiresAtUtc);
public record CreateApiTokenResponse(string Id, string Name, string RawToken, string TokenPrefix, DateTime? ExpiresAtUtc, DateTime CreatedAtUtc);

public static class CreateApiTokenRoute
{
    public static RouteHandlerBuilder AddCreateApiTokenRoute(this IEndpointRouteBuilder app)
    {
        return app.MapPost("/", async (
            CreateApiTokenRequest request,
            ICurrentUserAccessor currentUserAccessor,
            IApiTokenService apiTokenService) =>
        {
            var userId = currentUserAccessor.CurrentUserId;
            userId.ThrowIfNull(nameof(userId));

            var (token, rawToken) = await apiTokenService.CreateTokenAsync(
                userId,
                request.Name,
                request.ExpiresAtUtc);

            return TypedResults.Ok(new CreateApiTokenResponse(
                token.Id.ToString(),
                token.Name,
                rawToken,
                token.TokenPrefix,
                token.ExpiresAtUtc,
                token.CreatedAtUtc));
        });
    }
}
