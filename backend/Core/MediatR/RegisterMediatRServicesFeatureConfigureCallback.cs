using Core.Feature;
using MediatR.Registration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Core.MediatR;

public class RegisterMediatrServicesFeatureConfigureCallback : IFeatureConfigureCallback
{
    void IFeatureConfigureCallback.Configure(Type type, WebApplicationBuilder builder)
        => ServiceRegistrar.AddMediatRClasses(
            builder.Services,
            new MediatRServiceConfiguration()
                .RegisterServicesFromAssemblyContaining(type));
}