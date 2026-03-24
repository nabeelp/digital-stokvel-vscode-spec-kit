using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Core.Entities;

namespace DigitalStokvel.Infrastructure.Data;

/// <summary>
/// Main Entity Framework DbContext for the Digital Stokvel application
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Infrastructure entities
    public DbSet<IdempotencyLog> IdempotencyLogs => Set<IdempotencyLog>();

    // Domain entities will be added as they are created
    // Example: public DbSet<Member> Members => Set<Member>();
    // Example: public DbSet<StokvelsGroup> StokvelsGroups => Set<StokvelsGroup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure IdempotencyLog
        modelBuilder.Entity<IdempotencyLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
            entity.Property(e => e.IdempotencyKey).IsRequired().HasMaxLength(255);
            entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
        });

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

