using Database.Data;
using Mail.Data.Configurations;
using Mail.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Mail.Data;

public class MailDbContext(
    DbContextOptions<MailDbContext> options,
    IMediator mediator) : BaseDbContext<MailDbContext>("mail", options, mediator)
{
    public DbSet<SentEmail> SentEmails => Set<SentEmail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new SentEmailConfiguration());
    }
}
