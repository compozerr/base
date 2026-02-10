using Auth.Services;
using Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Auth.Endpoints.ApiTokens;

public record ApiTokenListItem(string Id, string Name, string TokenPrefix, DateTime? ExpiresAtUtc, DateTime? LastUsedAtUtc, DateTime CreatedAtUtc);
public record ListApiTokensResponse(List<ApiTokenListItem> Tokens);

public static class ListApiTokensRoute
{
    public static RouteHandlerBuilder AddListApiTokensRoute(this IEndpointRouteBuilder app)
    {
        return app.MapGet("/", async (
            ICurrentUserAccessor currentUserAccessor,
            IApiTokenService apiTokenService) =>
        {
            var userId = currentUserAccessor.CurrentUserId;
            userId.ThrowIfNull(nameof(userId));

            var tokens = await apiTokenService.GetTokensForUserAsync(userId);

            return TypedResults.Ok(new ListApiTokensResponse(
                tokens.Select(t => new ApiTokenListItem(
                    t.Id.ToString(),
                    t.Name,
                    t.TokenPrefix,
                    t.ExpiresAtUtc,
                    t.LastUsedAtUtc,
                    t.CreatedAtUtc)).ToList()));
        });
    }
}
