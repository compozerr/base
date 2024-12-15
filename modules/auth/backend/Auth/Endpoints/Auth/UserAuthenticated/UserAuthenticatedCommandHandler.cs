using System.Security.Claims;
using Auth.Endpoints.UserLogins.Create;
using Auth.Endpoints.Users.Create;
using Auth.Endpoints.Users.Update;
using Auth.Models;
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

        var email = principal.FindFirst(ClaimTypes.Email)!.Value;
        var avatarUrl = principal.FindFirst("urn:github:avatar")?.Value;
        var authProviderUserId = principal.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        var name = principal.Identity?.Name ?? email;
        var accessToken = userAuthenticatedCommand.GithubAuthenticationProperties?.GetAccessToken();

        ArgumentException.ThrowIfNullOrEmpty(email, nameof(email));
        ArgumentException.ThrowIfNullOrEmpty(avatarUrl, nameof(avatarUrl));
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentException.ThrowIfNullOrEmpty(accessToken, nameof(accessToken));

        UserId userId;

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
            userId = updatedUserResponse.Id;
        }
        else
        {
            var command = new CreateUserCommand(
                Name: name,
                Email: email,
                AvatarUrl: avatarUrl
            );

            var createdUserResponse = await mediator.Send(command, cancellationToken);

            userId = createdUserResponse.Id;
        }

        var createUserLoginCommand = new UpsertUserLoginCommand(
            UserId: userId,
            Provider: Provider.GitHub,
            ProviderUserId: authProviderUserId,
            AccessToken: accessToken
        );

        await mediator.Send(createUserLoginCommand, cancellationToken);

        return userId;
    }
}