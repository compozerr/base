using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Feature;

public interface IFeature
{
    public bool IsEnabled { get => true; }

    /// <summary>
    /// Controls the order in which features run lifecycle methods (ConfigureApp, etc.).
    /// Lower values run first. Default is 0.
    /// </summary>
    public int Order { get => 0; }

    public virtual void ConfigureBuilder(WebApplicationBuilder builder) { }
    public virtual void ConfigureApp(WebApplication app) { }
    public virtual void AfterAllMigrations(WebApplication app) { }
    public virtual void ConfigureServices(IServiceCollection services) { }
    public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration) { }
}
