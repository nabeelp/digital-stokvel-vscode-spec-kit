using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Enums;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DigitalStokvel.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of payout repository
/// </summary>
public class PayoutRepository : IPayoutRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PayoutRepository> _logger;

    public PayoutRepository(ApplicationDbContext context, ILogger<PayoutRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Payout> CreatePayoutAsync(Payout payout, CancellationToken cancellationToken = default)
    {
        _context.Payouts.Add(payout);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation(
            "Created payout {PayoutId} for group {GroupId} with {RecipientCount} recipients",
            payout.Id, payout.GroupId, payout.Recipients.Count);
            
        return payout;
    }

    public async Task<Payout?> GetPayoutByIdAsync(Guid payoutId, CancellationToken cancellationToken = default)
    {
        return await _context.Payouts
            .Include(p => p.Group)
            .Include(p => p.InitiatedByMember)
            .Include(p => p.ConfirmedByMember)
            .Include(p => p.Recipients)
                .ThenInclude(r => r.Member)
            .FirstOrDefaultAsync(p => p.Id == payoutId, cancellationToken);
    }

    public async Task<IEnumerable<Payout>> GetGroupPayoutsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _context.Payouts
            .Include(p => p.InitiatedByMember)
            .Include(p => p.ConfirmedByMember)
            .Include(p => p.Recipients)
                .ThenInclude(r => r.Member)
            .Where(p => p.GroupId == groupId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Payout>> GetPayoutsByStatusAsync(PayoutStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Payouts
            .Include(p => p.Group)
            .Include(p => p.Recipients)
            .Where(p => p.Status == status)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdatePayoutStatusAsync(Guid payoutId, PayoutStatus status, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        var payout = await _context.Payouts.FindAsync(new object[] { payoutId }, cancellationToken);
        if (payout == null)
        {
            throw new KeyNotFoundException($"Payout {payoutId} not found");
        }

        payout.Status = status;
        payout.ErrorMessage = errorMessage;
        payout.ModifiedAt = DateTime.UtcNow;

        if (status == PayoutStatus.Completed)
        {
            payout.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Updated payout {PayoutId} status to {Status}", payoutId, status);
    }

    public async Task RecordDisbursementAsync(Guid recipientId, string eftReference, CancellationToken cancellationToken = default)
    {
        var recipient = await _context.PayoutRecipients.FindAsync(new object[] { recipientId }, cancellationToken);
        if (recipient == null)
        {
            throw new KeyNotFoundException($"Payout recipient {recipientId} not found");
        }

        recipient.EftReference = eftReference;
        recipient.DisbursedAt = DateTime.UtcNow;
        recipient.IsSuccessful = true;
        recipient.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation(
            "Recorded disbursement for recipient {RecipientId} with EFT reference {EftReference}",
            recipientId, eftReference);
    }

    public async Task RecordDisbursementFailureAsync(Guid recipientId, string errorMessage, CancellationToken cancellationToken = default)
    {
        var recipient = await _context.PayoutRecipients.FindAsync(new object[] { recipientId }, cancellationToken);
        if (recipient == null)
        {
            throw new KeyNotFoundException($"Payout recipient {recipientId} not found");
        }

        recipient.IsSuccessful = false;
        recipient.ErrorMessage = errorMessage;
        recipient.RetryCount++;
        recipient.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogWarning(
            "Recorded disbursement failure for recipient {RecipientId}: {ErrorMessage}",
            recipientId, errorMessage);
    }

    public async Task<IEnumerable<Payout>> GetPendingApprovalsAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _context.Payouts
            .Include(p => p.InitiatedByMember)
            .Include(p => p.Recipients)
                .ThenInclude(r => r.Member)
            .Where(p => p.GroupId == groupId && 
                       (p.Status == PayoutStatus.PendingTreasurerApproval || 
                        p.Status == PayoutStatus.PendingQuorum))
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
