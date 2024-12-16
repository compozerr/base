
using Database.Data;
using Github.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Github.Data;

public class GithubDbContext(
    DbContextOptions<GithubDbContext> options,
    IMediator mediator) : BaseDbContext<GithubDbContext>("github", options, mediator)
{
    public DbSet<GithubUserSettings> GithubUserSettings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


    }
}
