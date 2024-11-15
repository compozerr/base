using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Feature;

public interface IFeature {
    public void ConfigureServices(IServiceCollection services);
    public void ConfigureApp(WebApplication app);
}