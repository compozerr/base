namespace Cli.Features.Hosting;

public sealed record CertifyUrlRequest(string Url, string AppName, Platform Platform);