using System.Security.Claims;
using Auth.AuthProviders;
using Core.MediatR;

namespace Auth.Endpoints.Auth.UserAuthenticated;

public record UserAuthenticatedCommand(
    ClaimsPrincipal ClaimsPrincipal,
    GithubAuthenticationProperties? GithubAuthenticationProperties) : ICommand<UserId>;