
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
