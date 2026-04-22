using System.Net;
using System.Text.Json;
using Api.Abstractions;
using Api.Data;
using Api.Hosting.Services;
using NSubstitute;

namespace Api.Tests.Services;

public class HostingServerHttpClientTests
{
    private sealed class CapturingHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }
        public string? CapturedBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            if (request.Content is not null)
                CapturedBody = await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private static HostingServerHttpClient BuildClient(CapturingHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://test") };
        var server = new Server { Id = ServerId.CreateNew() };
        var crypto = Substitute.For<ICryptoService>();
        crypto.SignStringWithKey(Arg.Any<string>(), Arg.Any<string>()).Returns("sig");
        return new HostingServerHttpClient(httpClient, server, crypto);
    }

    [Fact]
    public async Task PostJsonAsync_SendsExactlyOneRequest()
    {
        var handler = new CapturingHandler();
        var client = BuildClient(handler);

        await client.PostJsonAsync("/exec", new { command = "echo a" });

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task PostJsonAsync_PreservesUnsafeRelaxedEscaping()
    {
        var handler = new CapturingHandler();
        var client = BuildClient(handler);

        await client.PostJsonAsync("/exec", new { command = "echo a && echo b" });

        Assert.NotNull(handler.CapturedBody);
        Assert.Contains("&&", handler.CapturedBody);
        Assert.DoesNotContain("\\u0026", handler.CapturedBody);
    }

    [Fact]
    public async Task PutJsonAsync_SendsExactlyOneRequest()
    {
        var handler = new CapturingHandler();
        var client = BuildClient(handler);

        await client.PutJsonAsync("/domains", new { projectId = "abc" });

        Assert.Equal(1, handler.CallCount);
    }

    // Hosting manager (TypeScript) reads serverTier.cores / serverTier.ramGb —
    // if properties serialize as PascalCase they become undefined and Proxmox
    // sees cores=NaN&memory=NaN (400 Parameter verification failed).
    [Fact]
    public async Task PostJsonAsync_SerializesNestedRecordsAsCamelCase()
    {
        var handler = new CapturingHandler();
        var client = BuildClient(handler);

        var payload = new
        {
            serverTier = ServerTiers.GetById(new ServerTierId("T4"))
        };
        await client.PostJsonAsync("/projects/deploy", payload);

        Assert.NotNull(handler.CapturedBody);
        Assert.Contains("\"cores\":", handler.CapturedBody);
        Assert.Contains("\"ramGb\":", handler.CapturedBody);
        Assert.DoesNotContain("\"Cores\":", handler.CapturedBody);
        Assert.DoesNotContain("\"RamGb\":", handler.CapturedBody);
    }
}
