using Auth.Models;
using Core.MediatR;

namespace Auth.Endpoints.UserLogins.Create;

public sealed record UpsertUserLoginCommand(
    UserId UserId,
    Provider Provider,
    string ProviderUserId,
    string AccessToken) : ICommand<UserLoginId>;