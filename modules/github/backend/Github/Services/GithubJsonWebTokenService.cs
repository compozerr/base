using Core.Services;
using Github.Options;
using Microsoft.Extensions.Options;

namespace Github.Services;

public interface IGithubJsonWebTokenService
{
    string CreateToken();
}

public sealed class GithubJsonWebTokenService(
    IJsonWebTokenService jsonWebTokenService,
    IOptions<GithubAppOptions> options,
    IDateTimeProvider dateTimeProvider) : IGithubJsonWebTokenService
{
    public string CreateToken()
    {
        return jsonWebTokenService.CreateToken(
            options.Value.PrivateKeyCertificateBase64,
            options.Value.AppId,
            dateTimeProvider.UtcNow.AddMinutes(10));
    }
}