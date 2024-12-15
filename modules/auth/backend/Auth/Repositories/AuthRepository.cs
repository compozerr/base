using Auth.Data;
using Auth.Models;
using Core.Services;
using Microsoft.EntityFrameworkCore;

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

public sealed class AuthRepository(
    AuthDbContext context) : IAuthRepository
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

        var existingUserLogin = await context.UserLogins.Where(uL => uL.UserId == userId && uL.Provider == provider)
                                                        .FirstOrDefaultAsync(cancellationToken);

        if (existingUserLogin is null)
        {
            context.UserLogins.Add(userLogin);
        }
        else
        {
            userLogin.Id = existingUserLogin.Id;
            userLogin.CreatedAtUtc = existingUserLogin.CreatedAtUtc;
            context.Entry(existingUserLogin).CurrentValues.SetValues(userLogin);
        }

        await context.SaveChangesAsync(cancellationToken);

        return userLogin.Id;
    }
}