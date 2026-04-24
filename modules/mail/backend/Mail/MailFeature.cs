using Core.Feature;
using Mail.Data;
using Mail.Repositories;
using Mail.Services;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mail;

public class MailFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MailDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), b =>
            {
                b.MigrationsAssembly(typeof(MailDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<ISentEmailRepository, SentEmailRepository>();

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

    void IFeature.ConfigureApp(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MailDbContext>();
        context.Database.Migrate();
    }
}
