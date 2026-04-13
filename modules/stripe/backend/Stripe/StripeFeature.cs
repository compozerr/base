using Core.Extensions;
using Core.Feature;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Stripe.Data;
using Stripe.Data.Repositories;
using Stripe.Options;
using Stripe.Services;

namespace Stripe;

public class StripeFeature : IFeature
{
    void IFeature.ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<StripeDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), b =>
            {
                b.MigrationsAssembly(typeof(StripeDbContext).Assembly.FullName);
            });
        });

        services.AddRequiredConfigurationOptions<StripeOptions>("Stripe");

        services.AddScoped<ICurrentStripeCustomerIdAccessor, CurrentStripeCustomerIdAccessor>();

        services.AddScoped<IStripeCustomerRepository, StripeCustomerRepository>();
        services.AddScoped<IPaymentFailureSagaRepository, PaymentFailureSagaRepository>();
        services.AddScoped<IPaymentMethodMissingSagaRepository, PaymentMethodMissingSagaRepository>();

        services.AddScoped<IPaymentMethodsService, PaymentMethodsService>();
        services.AddScoped<ISubscriptionsService, SubscriptionsService>();
        services.AddScoped<IInvoicesService, InvoicesService>();

        services.AddScoped<IPaymentFailureSagaOrchestrator, PaymentFailureSagaOrchestrator>();
        services.AddScoped<IPaymentMethodMissingSagaOrchestrator, PaymentMethodMissingSagaOrchestrator>();
        services.AddScoped<IStripeCustomerValidationService, StripeCustomerValidationService>();
        services.AddScoped<IStripeOrgResolver, StripeOrgResolverService>();
        services.AddScoped<StripeCustomerMetadataSyncService>();
    }

    void IFeature.ConfigureApp(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var stripeOptions = scope.ServiceProvider.GetRequiredService<IOptions<StripeOptions>>().Value;

        StripeConfiguration.ApiKey = stripeOptions.ApiKey;

        var context = scope.ServiceProvider.GetRequiredService<StripeDbContext>();

        context.Database.Migrate();

        // One-time Stripe metadata sync — runs in background to avoid blocking startup.
        // Idempotent: skips customers already tagged with Type=organization.
        var stoppingToken = app.Lifetime.ApplicationStopping;
        _ = Task.Run(async () =>
        {
            try
            {
                using var syncScope = app.Services.CreateScope();
                var syncService = syncScope.ServiceProvider.GetRequiredService<StripeCustomerMetadataSyncService>();
                await syncService.SyncAllCustomerMetadataAsync(stoppingToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to sync Stripe customer metadata on startup");
            }
        });
    }

    void IFeature.AfterAllMigrations(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StripeDbContext>();

        // Clean up orphaned StripeCustomer records
        context.Database.ExecuteSqlRaw(@"
            DELETE FROM stripe.""StripeCustomers"" sc
            WHERE NOT EXISTS (
                SELECT 1 FROM organizations.""Organizations"" o2
                WHERE o2.""Id""::text = sc.""InternalId""
            )
            AND NOT EXISTS (
                SELECT 1 FROM auth.""Users"" u
                WHERE u.""Id""::text = sc.""InternalId""
            );
        ");

        // Backfill: replace user IDs with their personal org IDs
        context.Database.ExecuteSqlRaw(@"
            UPDATE stripe.""StripeCustomers"" sc
            SET ""InternalId"" = o.""Id""::text
            FROM organizations.""Organizations"" o
            JOIN auth.""Users"" u ON o.""OwnerUserId"" = u.""Id""
            WHERE o.""IsPersonal"" = true
              AND sc.""InternalId"" = u.""Id""::text;
        ");
    }
}