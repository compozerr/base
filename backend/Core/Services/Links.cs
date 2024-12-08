using Core.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Core.Services;

public interface ILinks
{
    Uri Get(string endpointName, RouteValueDictionary? routeValues = null);

    Uri Get(string endpointName, IEnumerable<(string key, object? value)> routeValues)
        => Get(endpointName,
               RouteValueDictionary.FromArray(
                   [.. routeValues.Select(t => new KeyValuePair<string, object?>(t.key, t.value))]));
}

public class Links(
    IHttpContextAccessor httpContextAccessor,
    LinkGenerator linkGenerator)
    : ILinks
{
    public Uri Get(string endpointName, RouteValueDictionary? routeValues = null)
    {
        var uriStr = linkGenerator.GetUriByName(httpContextAccessor.HttpContext.ThrowIfNull(nameof(httpContextAccessor)),
                                                endpointName,
                                                routeValues)
                                  .ThrowIfNullOrWhiteSpace();
        return new Uri(uriStr);
    }
}