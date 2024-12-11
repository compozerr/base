using Auth.Repositories;
using Core.MediatR;

namespace Auth.Endpoints.UserLogins.Create;

public sealed class CreateUserLoginCommandHandler(IAuthRepository authRepository) : ICommandHandler<CreateUserLoginCommand, UserLoginId>
{
    public async Task<UserLoginId> Handle(CreateUserLoginCommand command, CancellationToken cancellationToken)
    {
        return await authRepository.CreateUserLoginAsync(
            command.UserId,
            command.Provider,
            command.ProviderUserId,
            command.AccessToken,
            command.ExpiresAt,
            cancellationToken);
    }
}