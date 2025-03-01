using System.Security.Cryptography;
using System.Text;
using Api.Abstractions;
using Api.Data;

namespace Api.Services;

public class ChildServerHttpClient(
    HttpClient httpClient,
    Server server,
    ICryptoService cryptoService)
{
    private string SignRequest(string content)
    {
        return cryptoService.SignStringWithKey(content, server.Id.Value.ToString());
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        string content = "";
        if (request.Content != null)
        {
            content = await request.Content.ReadAsStringAsync();
        }

        var signature = SignRequest(content);
        request.Headers.Add("x-signature", signature);

        return await httpClient.SendAsync(request);
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync(request);
    }

    public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        return await SendAsync(request);
    }

    public async Task<HttpResponseMessage> PutAsync(string url, HttpContent content)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = content
        };
        return await SendAsync(request);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        return await SendAsync(request);
    }
}