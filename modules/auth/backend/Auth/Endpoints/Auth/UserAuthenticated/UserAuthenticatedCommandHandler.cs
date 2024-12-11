using System.Security.Claims;
using Auth.Endpoints.Users.Create;
using Auth.Endpoints.Users.Update;
using Auth.Repositories;
using Core.Extensions;
using Core.MediatR;
using MediatR;

namespace Auth.Endpoints.Auth.UserAuthenticated;

public class UserAuthenticatedCommandHandler(
    IUserRepository userRepository,
    IMediator mediator) : ICommandHandler<UserAuthenticatedCommand, UserId>
{
    public async Task<UserId> Handle(
        UserAuthenticatedCommand userAuthenticatedCommand,
        CancellationToken cancellationToken = default)
    {
        var principal = userAuthenticatedCommand.ClaimsPrincipal;

        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        var avatarUrl = principal.FindFirst("urn:github:avatar")?.Value;
        var authProviderUserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var name = principal.Identity?.Name ?? email;
        var accessToken = userAuthenticatedCommand.GithubAuthenticationProperties?.GetAccessToken();
        var expiresAt = userAuthenticatedCommand.GithubAuthenticationProperties?.GetExpiresAt() ?? throw new ArgumentNullException(nameof(userAuthenticatedCommand), "ExpiresAt is required");

        ArgumentException.ThrowIfNullOrEmpty(email, nameof(email));
        ArgumentException.ThrowIfNullOrEmpty(avatarUrl, nameof(avatarUrl));
        ArgumentException.ThrowIfNullOrEmpty(authProviderUserId, nameof(authProviderUserId));
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentException.ThrowIfNullOrEmpty(accessToken, nameof(accessToken));

        if (await userRepository.ExistsByAuthProviderUserIdAsync(authProviderUserId, cancellationToken))
        {
            var user = await userRepository.GetByAuthProviderUserIdAsync(authProviderUserId, cancellationToken);
            user.ThrowIfNull("User not found");

            var command = new UpdateUserCommand(
                UserId: user.Id,
                Name: name,
                Email: email,
                AvatarUrl: avatarUrl
            );

            var updatedUserResponse = await mediator.Send(command, cancellationToken);
            return updatedUserResponse.Id;
        }
        else
        {
            var command = new CreateUserCommand(
                AuthProviderUserId: principal.FindFirst(ClaimTypes.NameIdentifier)!.Value,
                Name: name,
                Email: email,
                AvatarUrl: avatarUrl
            );

            var createdUserResponse = await mediator.Send(command, cancellationToken);

            return createdUserResponse.Id;
        }
    }
}