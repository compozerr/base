using System.Reflection;
using Core.Feature;
using MediatR.Registration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Core.MediatR;

public class RegisterMediatrServicesFeatureConfigureCallback : IFeatureConfigureCallback
{
    private static readonly HashSet<Assembly> assemblies = [];

    void IFeatureConfigureCallback.Configure(Type type, WebApplicationBuilder builder)
    {
        if (!assemblies.Add(type.Assembly)) return;

        ServiceRegistrar.AddMediatRClasses(
            builder.Services,
            new MediatRServiceConfiguration()
                .RegisterServicesFromAssemblyContaining(type));
    }
}