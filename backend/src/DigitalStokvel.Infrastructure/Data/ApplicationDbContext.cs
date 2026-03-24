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

    // Domain entities
    public DbSet<Member> Members => Set<Member>();
    public DbSet<StokvelsGroup> StokvelsGroups => Set<StokvelsGroup>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Contribution> Contributions => Set<Contribution>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<PayoutRecipient> PayoutRecipients => Set<PayoutRecipient>();

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

        // Configure Member
        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
            entity.HasIndex(e => e.ApplicationUserId).IsUnique();
            entity.HasIndex(e => e.BankCustomerId);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ApplicationUserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.PreferredLanguage).HasMaxLength(5);
        });

        // Configure StokvelsGroup
        modelBuilder.Entity<StokvelsGroup>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.GroupType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ContributionFrequency).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Constitution).HasColumnType("jsonb");
            
            // Configure Money value objects
            entity.OwnsOne(e => e.ContributionAmount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("ContributionAmount").HasPrecision(18, 4);
                money.Property(m => m.Currency).HasColumnName("ContributionCurrency").HasMaxLength(3);
            });
            
            entity.OwnsOne(e => e.Balance, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Balance").HasPrecision(18, 4);
                money.Property(m => m.Currency).HasColumnName("BalanceCurrency").HasMaxLength(3);
            });
        });

        // Configure GroupMember (join table)
        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.GroupId, e.MemberId }).IsUnique();
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            
            // Relationships
            entity.HasOne(e => e.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Member)
                .WithMany(m => m.GroupMemberships)
                .HasForeignKey(e => e.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Contribution
        modelBuilder.Entity<Contribution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.GroupId, e.Timestamp }); // For efficient ledger queries
            entity.HasIndex(e => new { e.MemberId, e.Timestamp }); // For member history
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.HasIndex(e => new { e.Status, e.NextRetryAt }); // For retry queries
            
            entity.Property(e => e.IdempotencyKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PaymentMethod).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.PaymentGatewayReference).HasMaxLength(255);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);
            
            // Configure Money value object
            entity.OwnsOne(e => e.Amount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(18, 4);
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
            });
            
            // Relationships
            entity.HasOne(e => e.Group)
                .WithMany()
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete contributions
                
            entity.HasOne(e => e.Member)
                .WithMany()
                .HasForeignKey(e => e.MemberId)
                .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete contributions
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

