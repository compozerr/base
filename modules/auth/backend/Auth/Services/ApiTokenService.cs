using System.Security.Cryptography;
using System.Text;
using Auth.Data;
using Auth.Models;
using Microsoft.EntityFrameworkCore;

namespace Auth.Services;

public interface IApiTokenService
{
    Task<(ApiToken token, string rawToken)> CreateTokenAsync(UserId userId, string name, DateTime? expiresAtUtc);
    Task<ApiToken?> ValidateTokenAsync(string rawToken);
    Task<List<ApiToken>> GetTokensForUserAsync(UserId userId);
    Task RevokeTokenAsync(ApiTokenId tokenId, UserId userId);
}

public class ApiTokenService(AuthDbContext dbContext) : IApiTokenService
{
    private const string TokenPrefix = "cmpz_";
    private const int RandomPartLength = 40;
    private const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public async Task<(ApiToken token, string rawToken)> CreateTokenAsync(UserId userId, string name, DateTime? expiresAtUtc)
    {
        var rawToken = GenerateRawToken();
        var hash = HashToken(rawToken);
        var prefix = rawToken[..10];

        var apiToken = new ApiToken
        {
            Name = name,
            UserId = userId,
            TokenHash = hash,
            TokenPrefix = prefix,
            ExpiresAtUtc = expiresAtUtc
        };

        dbContext.ApiTokens.Add(apiToken);
        await dbContext.SaveChangesAsync();

        return (apiToken, rawToken);
    }

    public async Task<ApiToken?> ValidateTokenAsync(string rawToken)
    {
        if (!rawToken.StartsWith(TokenPrefix))
            return null;

        var hash = HashToken(rawToken);

        var apiToken = await dbContext.ApiTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.DeletedAtUtc == null);

        if (apiToken is null)
            return null;

        if (apiToken.ExpiresAtUtc.HasValue && apiToken.ExpiresAtUtc.Value < DateTime.UtcNow)
            return null;

        apiToken.LastUsedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return apiToken;
    }

    public async Task<List<ApiToken>> GetTokensForUserAsync(UserId userId)
    {
        return await dbContext.ApiTokens
            .Where(t => t.UserId == userId && t.DeletedAtUtc == null)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task RevokeTokenAsync(ApiTokenId tokenId, UserId userId)
    {
        var token = await dbContext.ApiTokens
            .FirstOrDefaultAsync(t => t.Id == tokenId && t.UserId == userId && t.DeletedAtUtc == null);

        if (token is null)
            return;

        token.DeletedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    private static string GenerateRawToken()
    {
        var randomBytes = new byte[RandomPartLength];
        RandomNumberGenerator.Fill(randomBytes);

        var sb = new StringBuilder(TokenPrefix, TokenPrefix.Length + RandomPartLength);
        for (var i = 0; i < RandomPartLength; i++)
        {
            sb.Append(AllowedChars[randomBytes[i] % AllowedChars.Length]);
        }

        return sb.ToString();
    }

    private static string HashToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}
