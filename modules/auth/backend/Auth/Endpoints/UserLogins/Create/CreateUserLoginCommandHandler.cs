using Auth.Repositories;
using Core.MediatR;

namespace Auth.Endpoints.UserLogins.Create;

public sealed class CreateUserLoginCommandHandler(IAuthRepository authRepository) : ICommandHandler<UpsertUserLoginCommand, UserLoginId>
{
    public async Task<UserLoginId> Handle(UpsertUserLoginCommand command, CancellationToken cancellationToken)
    {
        return await authRepository.UpsertUserLoginAsync(
            command.UserId,
            command.Provider,
            command.ProviderUserId,
            command.AccessToken,
            cancellationToken);
    }
}