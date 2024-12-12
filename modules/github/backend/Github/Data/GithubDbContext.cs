
using Database.Data;
using Github.Model;
using Microsoft.EntityFrameworkCore;

namespace Github.Data;

public class GithubDbContext(DbContextOptions<GithubDbContext> options) : BaseDbContext(options, "github")
{
    public DbSet<Installation> Installations => Set<Installation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
