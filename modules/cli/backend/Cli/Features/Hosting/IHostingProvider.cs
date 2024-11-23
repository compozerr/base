using System.Runtime.ConstrainedExecution;

namespace Cli.Features.Hosting;

public interface IHostingProvider
{
    public Task<DeployResponse> DeployAsync(DeployRequest request);
    public Task<DestroyResponse> DestroyAsync(DestroyRequest request);
    public Task<CertifyUrlResponse> CertifyUrlAsync(CertifyUrlRequest request);
}
