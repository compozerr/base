
using Database.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Stripe.Data.Models;

namespace Stripe.Data;

public class StripeDbContext(
    DbContextOptions<StripeDbContext> options,
    IMediator mediator) : BaseDbContext<StripeDbContext>("stripe", options, mediator)
{
    public DbSet<StripeCustomer> StripeCustomers => Set<StripeCustomer>();
    public DbSet<PaymentFailureSaga> PaymentFailureSagas => Set<PaymentFailureSaga>();
    public DbSet<PaymentFailureSagaAuditLog> PaymentFailureSagaAuditLogs => Set<PaymentFailureSagaAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PaymentFailureSaga>()
            .HasIndex(s => s.SubscriptionId);

        modelBuilder.Entity<PaymentFailureSaga>()
            .HasIndex(s => new { s.SubscriptionId, s.Status });

        modelBuilder.Entity<PaymentFailureSagaAuditLog>()
            .HasIndex(a => a.SagaId);
    }
}
