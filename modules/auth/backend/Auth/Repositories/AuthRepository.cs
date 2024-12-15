using Auth.Data;
using Auth.Models;

namespace Auth.Repositories;

public interface IAuthRepository
{
    public Task<UserLoginId> UpsertUserLoginAsync(
        UserId userId,
        Provider provider,
        string providerUserId,
        string accessToken,
        CancellationToken cancellationToken = default);
}

public sealed class AuthRepository(AuthDbContext context) : IAuthRepository
{
    public async Task<UserLoginId> UpsertUserLoginAsync(
        UserId userId,
        Provider provider,
        string providerUserId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var userLogin = provider switch
        {
            Provider.GitHub => new GithubUserLogin
            {
                UserId = userId,
                Provider = provider,
                ProviderUserId = providerUserId,
                AccessToken = accessToken
            },
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        };

        var existingUserLogin = await context.UserLogins.FindAsync(userId);

        if (userLogin is null)
        {
            context.UserLogins.Add(userLogin);
        }
        else
        {
            context.Entry(existingUserLogin).CurrentValues.SetValues(userLogin);
        }

        await context.SaveChangesAsync(cancellationToken);

        return userLogin.Id;
    }
}