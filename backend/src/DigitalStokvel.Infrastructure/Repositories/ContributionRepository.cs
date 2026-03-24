using Microsoft.EntityFrameworkCore;
using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Enums;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Infrastructure.Data;

namespace DigitalStokvel.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Contribution entity with indexed queries
/// </summary>
public class ContributionRepository : Repository<Contribution>, IContributionRepository
{
    public ContributionRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Adds a new contribution to the ledger
    /// </summary>
    public async Task<Contribution> AddContributionAsync(Contribution contribution, CancellationToken cancellationToken = default)
    {
        await _context.Set<Contribution>().AddAsync(contribution, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return contribution;
    }

    /// <summary>
    /// Gets the contribution ledger for a group with pagination
    /// Indexed on (group_id, timestamp DESC) for efficient querying
    /// </summary>
    public async Task<(IEnumerable<Contribution> Contributions, int TotalCount)> GetGroupLedgerAsync(
        Guid groupId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Contribution>()
            .Where(c => c.GroupId == groupId)
            .Include(c => c.Member)
            .OrderByDescending(c => c.Timestamp);

        var totalCount = await query.CountAsync(cancellationToken);

        var contributions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (contributions, totalCount);
    }

    /// <summary>
    /// Gets a member's contribution history across all groups
    /// Indexed on (member_id, timestamp DESC)
    /// </summary>
    public async Task<(IEnumerable<Contribution> Contributions, int TotalCount)> GetMemberHistoryAsync(
        Guid memberId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Contribution>()
            .Where(c => c.MemberId == memberId)
            .Include(c => c.Group)
            .OrderByDescending(c => c.Timestamp);

        var totalCount = await query.CountAsync(cancellationToken);

        var contributions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (contributions, totalCount);
    }

    /// <summary>
    /// Checks if an idempotency key exists (prevents duplicate transactions)
    /// </summary>
    public async Task<bool> IdempotencyKeyExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Contribution>()
            .AnyAsync(c => c.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    /// <summary>
    /// Gets a contribution by idempotency key for returning cached result
    /// </summary>
    public async Task<Contribution?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Contribution>()
            .Include(c => c.Group)
            .Include(c => c.Member)
            .FirstOrDefaultAsync(c => c.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    /// <summary>
    /// Gets all pending debit orders due for retry (48-hour delay elapsed)
    /// </summary>
    public async Task<IEnumerable<Contribution>> GetPendingRetryContributionsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.Set<Contribution>()
            .Where(c => 
                c.Status == ContributionStatus.Retrying &&
                c.RetryCount < 2 && // Max 2 retries
                c.NextRetryAt != null &&
                c.NextRetryAt <= now)
            .Include(c => c.Group)
            .Include(c => c.Member)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets total contributions for a group (completed only)
    /// </summary>
    public async Task<decimal> GetTotalGroupContributionsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var total = await _context.Set<Contribution>()
            .Where(c => c.GroupId == groupId && c.Status == ContributionStatus.Completed)
            .SumAsync(c => c.Amount.Amount, cancellationToken);

        return total;
    }

    /// <summary>
    /// Gets total contributions for a member in a specific group (completed only)
    /// </summary>
    public async Task<decimal> GetMemberGroupContributionsAsync(Guid memberId, Guid groupId, CancellationToken cancellationToken = default)
    {
        var total = await _context.Set<Contribution>()
            .Where(c => 
                c.MemberId == memberId && 
                c.GroupId == groupId && 
                c.Status == ContributionStatus.Completed)
            .SumAsync(c => c.Amount.Amount, cancellationToken);

        return total;
    }
}
