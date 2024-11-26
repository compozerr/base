namespace Cli.Services.Hosting;

public sealed record CertifyUrlRequest(string Url, string AppName, Platform Platform);