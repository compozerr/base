using Database.Data;
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

        modelBuilder.Entity<SentEmail>(entity =>
        {
            entity.HasIndex(e => e.SentAtUtc);
            entity.Property(e => e.Status).HasConversion<int>();
        });
    }
}
