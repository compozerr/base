using Microsoft.AspNetCore.Http;

namespace Core.Helpers;

/// <summary>
/// Interface for accessing server URL information
/// </summary>
public interface IMyServerUrlAccessor
{
    /// <summary>
    /// Gets the base server URL as a Uri object
    /// </summary>
    Uri GetServerUrl();

    /// <summary>
    /// Combines the server URL with a relative path and returns a Uri object
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the resulting URL is invalid</exception>
    Uri CombineWithPath(string relativePath);
}

/// <summary>
/// Provides access to the current server's URL information
/// </summary>
public class MyServerUrlAccessor(IHttpContextAccessor httpContextAccessor) : IMyServerUrlAccessor
{
    public Uri GetServerUrl()
    {
        var request = httpContextAccessor.HttpContext!.Request;
        var host = request.Host.Value;
        var scheme = request.Scheme;

        return new Uri($"{scheme}://{host}");
    }

    public Uri CombineWithPath(string relativePath)
    {
        ArgumentNullException.ThrowIfNull(relativePath);

        var baseUrl = GetServerUrl();
        if (!Uri.TryCreate(baseUrl, relativePath.TrimStart('/'), out var combinedUri))
        {
            throw new ArgumentException("Invalid path combination", nameof(relativePath));
        }

        return combinedUri;
    }
}