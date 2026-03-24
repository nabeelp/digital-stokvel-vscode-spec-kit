using Microsoft.EntityFrameworkCore;
using DigitalStokvel.Core.Interfaces;

namespace DigitalStokvel.Infrastructure.Data;

/// <summary>
/// Main Entity Framework DbContext for the Digital Stokvel application
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets will be added as entities are created
    // Example: public DbSet<Member> Members => Set<Member>();
    // Example: public DbSet<StokvelsGroup> StokvelsGroups => Set<StokvelsGroup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filters and conventions will be added here
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Interceptors are registered in Program.cs via AddInterceptors()
        // This allows proper dependency injection of IHttpContextAccessor
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit fields are populated by AuditInterceptor
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        // Audit fields are populated by AuditInterceptor
        return base.SaveChanges();
    }
}
