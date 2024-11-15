using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Feature;

public interface IFeature {
    public virtual void ConfigureServices(IServiceCollection services) {}
    public virtual void ConfigureApp(WebApplication app) {}
    public virtual void AddRoutes(IEndpointRouteBuilder app) {}
}