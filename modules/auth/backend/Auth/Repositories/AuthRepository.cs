using Auth.Data;
using Auth.Models;

namespace Auth.Repositories;

public interface IAuthRepository
{
    public Task<UserLoginId> CreateUserLoginAsync(
        UserId userId,
        Provider provider,
        string providerUserId,
        string accessToken,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);
}

public sealed class AuthRepository(AuthDbContext context) : IAuthRepository
{
    public async Task<UserLoginId> CreateUserLoginAsync(
        UserId userId,
        Provider provider,
        string providerUserId,
        string accessToken,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        var userLogin = provider switch
        {
            Provider.GitHub => new GithubUserLogin
            {
                UserId = userId,
                Provider = provider,
                ProviderUserId = providerUserId,
                AccessToken = accessToken,
                ExpiresAtUtc = expiresAt
            },
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        };

        await context.UserLogins.AddAsync(userLogin, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return userLogin.Id;
    }
}