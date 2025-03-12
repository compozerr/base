using Microsoft.Extensions.Configuration;

namespace Core.Services;

public interface IFrontendLocation
{
    Uri GetFromPath(string path);
}

public sealed class FrontendLocation(IConfiguration configuration) : IFrontendLocation
{
    public Uri GetFromPath(string path)
    {
        var frontendBaseUrl = configuration["FRONTEND_URL"] ?? throw new InvalidOperationException("FRONTEND_URL is not set");

        var uri = new UriBuilder(frontendBaseUrl) { Path = path };

        return uri.Uri;
    }
}
