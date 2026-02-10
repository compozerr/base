using System.Security.Claims;
using System.Text.Encodings.Web;
using Auth.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Auth.Authentication;

public class ApiTokenAuthenticationDefaults
{
    public const string AuthenticationScheme = "ApiToken";
}

public class ApiTokenAuthenticationOptions : AuthenticationSchemeOptions
{
}

public class ApiTokenAuthenticationHandler(
    IOptionsMonitor<ApiTokenAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiTokenService apiTokenService)
    : AuthenticationHandler<ApiTokenAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer cmpz_"))
            return AuthenticateResult.NoResult();

        var token = authHeader["Bearer ".Length..].Trim();

        var apiToken = await apiTokenService.ValidateTokenAsync(token);
        if (apiToken is null)
            return AuthenticateResult.Fail("Invalid or expired API token");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiToken.UserId.ToString()),
            new(ClaimTypes.Name, apiToken.User?.Name ?? ""),
            new(ClaimTypes.Email, apiToken.User?.Email ?? ""),
        };

        if (apiToken.User?.Roles is not null)
        {
            foreach (var role in apiToken.User.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var identity = new ClaimsIdentity(claims, ApiTokenAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiTokenAuthenticationDefaults.AuthenticationScheme);

        return AuthenticateResult.Success(ticket);
    }
}
