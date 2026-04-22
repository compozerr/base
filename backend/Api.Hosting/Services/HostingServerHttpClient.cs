using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using Api.Data;

namespace Api.Hosting.Services;

public class HostingServerHttpClient(
    HttpClient httpClient,
    Server server,
    ICryptoService cryptoService)
{
    // JsonSerializerDefaults.Web matches JsonContent.Create's implicit default
    // (camelCase property naming) — the hosting/manager (Node/Express) reads
    // req.body.serverTier.cores / ramGb in lowercase, so PascalCase would send
    // cores=NaN&memory=NaN to Proxmox and 400 every deploy that updates VM HW.
    // Encoder = UnsafeRelaxedJsonEscaping so signer-side bytes match the
    // verifier's JSON.stringify output (which does not escape '<', '>', '&',
    // '\'' or non-ASCII).
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

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

    public Task<HttpResponseMessage> PostJsonAsync<T>(
        string url,
        T payload,
        CancellationToken cancellationToken = default)
        => PostAsync(url, JsonContent.Create(payload, options: SerializerOptions), cancellationToken);

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

    public Task<HttpResponseMessage> PutJsonAsync<T>(
        string url,
        T payload,
        CancellationToken cancellationToken = default)
        => PutAsync(url, JsonContent.Create(payload, options: SerializerOptions), cancellationToken);

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
