using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Enums;

namespace DigitalStokvel.Core.Interfaces;

/// <summary>
/// Repository interface for payout operations
/// </summary>
public interface IPayoutRepository
{
    /// <summary>
    /// Creates a new payout with recipients
    /// </summary>
    Task<Payout> CreatePayoutAsync(Payout payout, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a payout by ID with all related entities loaded
    /// </summary>
    Task<Payout?> GetPayoutByIdAsync(Guid payoutId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all payouts for a group
    /// </summary>
    Task<IEnumerable<Payout>> GetGroupPayoutsAsync(Guid groupId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets payouts by status
    /// </summary>
    Task<IEnumerable<Payout>> GetPayoutsByStatusAsync(PayoutStatus status, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates payout status
    /// </summary>
    Task UpdatePayoutStatusAsync(Guid payoutId, PayoutStatus status, string? errorMessage = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Records successful disbursement for a recipient
    /// </summary>
    Task RecordDisbursementAsync(Guid recipientId, string eftReference, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Records failed disbursement for a recipient
    /// </summary>
    Task RecordDisbursementFailureAsync(Guid recipientId, string errorMessage, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets pending payouts requiring Treasurer approval
    /// </summary>
    Task<IEnumerable<Payout>> GetPendingApprovalsAsync(Guid groupId, CancellationToken cancellationToken = default);
}
