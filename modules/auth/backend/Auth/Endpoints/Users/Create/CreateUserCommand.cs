using Core.MediatR;

namespace Auth.Endpoints.Users.Create;

public sealed record CreateUserCommand(
    string AuthProviderUserId,
    string Name,
    string Email,
    string AvatarUrl) : ICommand<CreateUserResponse>;
