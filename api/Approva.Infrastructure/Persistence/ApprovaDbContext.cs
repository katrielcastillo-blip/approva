using Microsoft.EntityFrameworkCore;

namespace Approva.Infrastructure.Persistence;

public class ApprovaDbContext : DbContext
{
    public ApprovaDbContext(DbContextOptions<ApprovaDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApprovaDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
