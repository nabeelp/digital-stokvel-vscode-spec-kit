using DigitalStokvel.Core.Entities;

namespace DigitalStokvel.Core.Interfaces;

/// <summary>
/// Repository interface for Contribution entity operations
/// </summary>
public interface IContributionRepository : IRepository<Contribution>
{
    /// <summary>
    /// Adds a new contribution to the ledger
    /// </summary>
    Task<Contribution> AddContributionAsync(Contribution contribution, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the contribution ledger for a group with pagination
    /// </summary>
    /// <param name="groupId">Group identifier</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of contributions ordered by timestamp descending</returns>
    Task<(IEnumerable<Contribution> Contributions, int TotalCount)> GetGroupLedgerAsync(
        Guid groupId, 
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a member's contribution history across all groups
    /// </summary>
    /// <param name="memberId">Member identifier</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of contributions ordered by timestamp descending</returns>
    Task<(IEnumerable<Contribution> Contributions, int TotalCount)> GetMemberHistoryAsync(
        Guid memberId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an idempotency key exists (to prevent duplicate transactions)
    /// </summary>
    Task<bool> IdempotencyKeyExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a contribution by idempotency key
    /// </summary>
    Task<Contribution?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending debit orders that are due for retry
    /// </summary>
    Task<IEnumerable<Contribution>> GetPendingRetryContributionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total contributions for a group
    /// </summary>
    Task<decimal> GetTotalGroupContributionsAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total contributions for a member in a specific group
    /// </summary>
    Task<decimal> GetMemberGroupContributionsAsync(Guid memberId, Guid groupId, CancellationToken cancellationToken = default);
}
