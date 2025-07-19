using Api.Data;

namespace Api.Hosting.Services;

public class HostingServerHttpClient(
    HttpClient httpClient,
    Server server,
    ICryptoService cryptoService)
{
    public string BaseDomain => httpClient.BaseAddress!.ToString();

    public void SetRequestTimeout(TimeSpan timeout)
    {
        httpClient.Timeout = timeout;
    }
    
    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        string content = "";
        if (request.Content != null)
        {
            content = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        var signature = SignRequest(content);
        request.Headers.Add("x-signature", signature);

        return await httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> GetAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PostAsync(
        string url,
        HttpContent content,
        CancellationToken cancellationToken = default)
    {   
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        return await SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> PutAsync(
        string url,
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = content
        };
        return await SendAsync(request, cancellationToken);
    }

    public async Task<HttpResponseMessage> DeleteAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        return await SendAsync(request, cancellationToken);
    }

    private string SignRequest(string content)
    {
        return cryptoService.SignStringWithKey(content, server.Id.Value.ToString());
    }
}