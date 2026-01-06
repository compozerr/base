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
        var frontendBaseUrl = configuration["FrontendUrl"] ?? throw new InvalidOperationException("FRONTEND_URL is not set");

        var inputUri = new Uri(path, UriKind.RelativeOrAbsolute);
        var builder = new UriBuilder(frontendBaseUrl);

        if (inputUri.IsAbsoluteUri)
        {
            builder.Path = inputUri.AbsolutePath;
            builder.Query = inputUri.Query;
            builder.Fragment = inputUri.Fragment;
        }
        else
        {
            var pathAndQuery = path.Split('?', 2);
            builder.Path = pathAndQuery[0];
            if (pathAndQuery.Length > 1)
            {
                builder.Query = pathAndQuery[1];
            }
        }

        return builder.Uri;
    }
}
