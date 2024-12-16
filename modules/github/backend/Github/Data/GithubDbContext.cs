
using Database.Data;
using Github.Models;
using Microsoft.EntityFrameworkCore;

namespace Github.Data;

public class GithubDbContext(DbContextOptions<GithubDbContext> options) : BaseDbContext<GithubDbContext>(options, "github")
{
    public DbSet<GithubUserSettings> GithubUserSettings { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
