using System.Security.Claims;
using Auth.Endpoints.Users.Create;
using Auth.Repositories;
using Core.MediatR;
using MediatR;

namespace Auth.Endpoints.Auth;

public class UserAuthenticatedCommandHandler(
    IUserRepository userRepository,
    IMediator mediator) : ICommandHandler<UserAuthenticatedCommand>
{
    public async Task Handle(UserAuthenticatedCommand userAuthenticatedCommand, CancellationToken cancellationToken = default)
    {
        var principal = userAuthenticatedCommand.ClaimsPrincipal;

        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        var avatarUrl = principal.FindFirst("urn:github:avatar")?.Value;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(avatarUrl))
            return;

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
            return;

        var command = new CreateUserCommand(
            Name: principal.Identity?.Name ?? email,
            Email: email,
            AvatarUrl: avatarUrl
        );

        await mediator.Send(command, cancellationToken);
    }
}