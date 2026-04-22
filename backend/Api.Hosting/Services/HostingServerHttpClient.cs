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
    // The hosting/manager (Node/Express) verifier re-stringifies req.body via
    // JSON.stringify for signature comparison. JS's JSON.stringify does not
    // escape '<', '>', '&', '\'' or non-ASCII, whereas System.Text.Json's default
    // JavaScriptEncoder does (emitting <, &, ', etc.). Signing
    // the default-encoded bytes but verifying against the re-stringified
    // (non-escaped) bytes produces spurious 401 Unauthorized whenever the
    // payload contains any of those characters (e.g. URLs with '&' in query
    // strings, commands using shell ''' or '&&'). Use UnsafeRelaxedJsonEscaping
    // so the bytes we sign match the bytes the verifier sees after parse+
    // re-stringify.
    public static readonly JsonSerializerOptions SerializerOptions = new()
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
