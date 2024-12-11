using Auth.Models;
using Core.MediatR;

namespace Auth.Endpoints.UserLogins.Create;

public sealed record CreateUserLoginCommand(
    UserId UserId,
    Provider Provider,
    string ProviderUserId,
    string AccessToken,
    DateTime ExpiresAt) : ICommand<UserLoginId>;