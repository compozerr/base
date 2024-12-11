using Microsoft.AspNetCore.Authentication;

namespace Auth.AuthProviders;

public sealed class GithubAuthenticationProperties(AuthenticationProperties? authenticationProperties)
{
    public string? GetAccessToken()
        => authenticationProperties?.GetTokenValue("access_token");

    public DateTime? GetExpiresAt()
        => DateTime.TryParse(authenticationProperties?.GetString(".expires"), out var expiresAt) ? expiresAt : null;
}