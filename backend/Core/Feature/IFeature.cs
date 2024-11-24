using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Feature;

public interface IFeature
{
    public virtual void ConfigureBuilder(WebApplicationBuilder builder) { }
    public virtual void ConfigureApp(WebApplication app) { }
    public virtual void ConfigureServices(IServiceCollection services) { }
    public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
}