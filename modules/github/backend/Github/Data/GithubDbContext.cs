
using Database.Data;
using Microsoft.EntityFrameworkCore;

namespace Github.Data;

public class GithubDbContext(DbContextOptions<GithubDbContext> options) : BaseDbContext<GithubDbContext>(options, "github")
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
