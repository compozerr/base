using Core.Feature;
using Microsoft.Extensions.DependencyInjection;
using Mail.Services;
using MediatR;
using Microsoft.Extensions.Hosting;

namespace Mail;

public class MailFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services)
    {

        services.AddTransient<IMailService>(s =>
        {
            var hostEnvironment = s.GetRequiredService<IHostEnvironment>();

            if (hostEnvironment.IsDevelopment())
            {
                return new MockMailService();
            }

            var publisher = s.GetRequiredService<IPublisher>();
            var mailService = new MailService(publisher);
            return mailService;
        });
    }
}