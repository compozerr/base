using System.Security.Claims;
using Core.MediatR;

namespace Auth.Endpoints.Auth;

public record UserAuthenticatedCommand(ClaimsPrincipal ClaimsPrincipal) : ICommand;