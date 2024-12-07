using System.Security.Claims;
using Auth.Endpoints.Users.Create;
using Auth.Repositories;
using Core.MediatR;
using MediatR;

namespace Auth.Endpoints.Auth;

public class UserAuthenticatedCommandHandler(
    IUserRepository userRepository,
    IMediator mediator) : ICommandHandler<UserAuthenticatedCommand, UserId>
{
    public async Task<UserId> Handle(UserAuthenticatedCommand userAuthenticatedCommand, CancellationToken cancellationToken = default)
    {
        var principal = userAuthenticatedCommand.ClaimsPrincipal;

        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        var avatarUrl = principal.FindFirst("urn:github:avatar")?.Value;

        ArgumentException.ThrowIfNullOrEmpty(email, nameof(email));
        ArgumentException.ThrowIfNullOrEmpty(avatarUrl, nameof(avatarUrl));

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
            return (await userRepository.GetByEmailAsync(email, cancellationToken))!.Id;

        var command = new CreateUserCommand(
            Name: principal.Identity?.Name ?? email,
            Email: email,
            AvatarUrl: avatarUrl
        );

        var createdUserResponse = await mediator.Send(command, cancellationToken);

        return createdUserResponse.Id;
    }
}