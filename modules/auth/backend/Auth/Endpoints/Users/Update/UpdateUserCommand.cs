using Core.MediatR;

namespace Auth.Endpoints.Users.Update;

public sealed record UpdateUserCommand(
    UserId UserId,
    string Name,
    string Email,
    string AvatarUrl) : ICommand<UpdateUserResponse>;