using Api.Abstractions;

namespace Api.Hosting.Services;

public sealed class HostingApi(
    ServerId ServerId,
    IHostingServerHttpClientFactory hostingServerHttpClientFactory)
{
    private HostingServerHttpClient? HttpClient { get; set; }

    internal async Task<HostingApi> Initialize()
    {
        if (HttpClient is { }) return this;

        HttpClient = await hostingServerHttpClientFactory.GetHostingServerHttpClientAsync(ServerId);

        return this;
    }
}