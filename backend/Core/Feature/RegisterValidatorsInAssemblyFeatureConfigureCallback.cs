using FluentValidation;

namespace Core.Feature;

public class RegisterValidatorsInAssemblyFeatureConfigureCallback : IFeatureConfigureCallback
{
    void IFeatureConfigureCallback.Configure(Type type, Microsoft.AspNetCore.Builder.WebApplicationBuilder builder)
        => builder.Services.AddValidatorsFromAssembly(type.Assembly);
}