using Core.Feature;
using Microsoft.Extensions.DependencyInjection;
using MailResend.Services;
using Core.Extensions;
using MailResend.Options;

namespace MailResend;

public class MailResendFeature : IFeature
{

    void IFeature.ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IResendService, ResendService>();

        services.AddRequiredConfigurationOptions<MailResendOptions>("MailResend");
    }
}