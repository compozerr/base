using Microsoft.AspNetCore.Builder;

namespace Core.Feature;

public interface IFeatureConfigureCallback
{
    void Configure(Type type) { }
    void Configure(Type type, WebApplication app) { }
    void Configure(Type type, WebApplicationBuilder builder) { }
}