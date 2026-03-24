using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Enums;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DigitalStokvel.Services;

/// <summary>
/// Service for managing group payouts with role-based authorization
/// </summary>
public class PayoutService
{
    private readonly IPayoutRepository _payoutRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ISmsNotificationService _smsNotificationService;
    private readonly ILogger<PayoutService> _logger;

    public PayoutService(
        IPayoutRepository payoutRepository,
        IGroupRepository groupRepository,
        IPaymentGateway paymentGateway,
        ISmsNotificationService smsNotificationService,
        ILogger<PayoutService> logger)
    {
        _payoutRepository = payoutRepository;
        _groupRepository = groupRepository;
        _paymentGateway = paymentGateway;
        _smsNotificationService = smsNotificationService;
        _logger = logger;
    }

    /// <summary>
    /// Initiates a payout (Chairperson only)
    /// </summary>
    public async Task<(bool Success, Guid? PayoutId, string? ErrorMessage)> InitiatePayoutAsync(
        Guid groupId,
        Guid initiatingMemberId,
        PayoutType payoutType,
        Guid? specificRecipientId = null,
        decimal? customAmount = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate Chairperson role
            var isChairperson = await _groupRepository.HasRoleAsync(
                groupId, initiatingMemberId, "Chairperson", cancellationToken);
                
            if (!isChairperson)
            {
                return (false, null, "Only Chairperson can initiate payouts");
            }

            // Get group with members
            var group = await _groupRepository.GetGroupWithMembersAsync(groupId, cancellationToken);
            if (group == null)
            {
                return (false, null, "Group not found");
            }

            // Calculate payout amounts based on type
            var recipients = payoutType switch
            {
                PayoutType.RotatingCycle => await CalculateRotatingPayoutAsync(
                    group, specificRecipientId, cancellationToken),
                    
                PayoutType.YearEndPot => await CalculateYearEndPayoutAsync(
                    group, cancellationToken),
                    
                PayoutType.PartialWithdrawal => CalculatePartialWithdrawalAsync(
                    group, specificRecipientId!.Value, customAmount!.Value),
                    
                _ => throw new ArgumentException($"Invalid payout type: {payoutType}")
            };

            if (!recipients.Any())
            {
                return (false, null, "No recipients calculated for payout");
            }

            // Calculate total amount
            var totalAmount = recipients.Sum(r => r.Amount.Amount);

            // Validate sufficient balance
            if (totalAmount > group.Balance.Amount)
            {
                return (false, null, $"Insufficient balance. Required: R{totalAmount:F2}, Available: R{group.Balance.Amount:F2}");
            }

            // Create payout entity
            var payout = new Payout
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                PayoutType = payoutType,
                TotalAmount = new Money(totalAmount),
                InitiatedBy = initiatingMemberId,
                Status = payoutType == PayoutType.PartialWithdrawal 
                    ? PayoutStatus.PendingQuorum 
                    : PayoutStatus.PendingTreasurerApproval,
                Reason = reason ?? $"{payoutType} payout - {DateTime.UtcNow:yyyy-MM-dd}",
                Recipients = recipients,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = initiatingMemberId.ToString()
            };

            // Save to database
            var createdPayout = await _payoutRepository.CreatePayoutAsync(payout, cancellationToken);

            _logger.LogInformation(
                "Payout initiated: {PayoutId} | Type: {PayoutType} | Group: {GroupId} | Amount: R{Amount} | Recipients: {RecipientCount}",
                createdPayout.Id, payoutType, groupId, totalAmount, recipients.Count);

            return (true, createdPayout.Id, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate payout for group {GroupId}", groupId);
            return (false, null, $"Failed to initiate payout: {ex.Message}");
        }
    }

    /// <summary>
    /// Confirms and executes a payout (Treasurer only)
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> ConfirmPayoutAsync(
        Guid payoutId,
        Guid treasurerMemberId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get payout with all details
            var payout = await _payoutRepository.GetPayoutByIdAsync(payoutId, cancellationToken);
            if (payout == null)
            {
                return (false, "Payout not found");
            }

            // Validate Treasurer role
            var isTreasurer = await _groupRepository.HasRoleAsync(
                payout.GroupId, treasurerMemberId, "Treasurer", cancellationToken);
                
            if (!isTreasurer)
            {
                return (false, "Only Treasurer can confirm payouts");
            }

            // Validate payout status
            if (payout.Status != PayoutStatus.PendingTreasurerApproval &&
                payout.Status != PayoutStatus.Approved)
            {
                return (false, $"Payout cannot be confirmed in status: {payout.Status}");
            }

            // Update to In Progress
            await _payoutRepository.UpdatePayoutStatusAsync(
                payoutId, PayoutStatus.InProgress, null, cancellationToken);

            payout.ConfirmedBy = treasurerMemberId;

            // Execute EFT disbursements
            var allSuccessful = true;
            var failureMessages = new List<string>();

            foreach (var recipient in payout.Recipients)
            {
                try
                {
                    // TODO: Get actual bank account details from Member entity
                    var bankAccountNumber = recipient.BankAccountNumber ?? "STUB_ACCOUNT";

                    // Execute EFT transfer via payment gateway
                    var eftResult = await ExecuteEftTransferAsync(
                        recipient.MemberId,
                        recipient.Amount.Amount,
                        $"Payout from {payout.Group.Name}",
                        cancellationToken);

                    if (eftResult.Success)
                    {
                        await _payoutRepository.RecordDisbursementAsync(
                            recipient.Id, 
                            eftResult.TransactionReference!, 
                            cancellationToken);

                        _logger.LogInformation(
                            "Disbursed R{Amount} to member {MemberId} | EFT Ref: {EftReference}",
                            recipient.Amount.Amount, recipient.MemberId, eftResult.TransactionReference);
                    }
                    else
                    {
                        await _payoutRepository.RecordDisbursementFailureAsync(
                            recipient.Id,
                            eftResult.ErrorMessage ?? "EFT transfer failed",
                            cancellationToken);

                        allSuccessful = false;
                        failureMessages.Add($"Member {recipient.Member.Member.PhoneNumber}: {eftResult.ErrorMessage}");

                        _logger.LogWarning(
                            "Failed to disburse R{Amount} to member {MemberId}: {Error}",
                            recipient.Amount.Amount, recipient.MemberId, eftResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    await _payoutRepository.RecordDisbursementFailureAsync(
                        recipient.Id,
                        ex.Message,
                        cancellationToken);

                    allSuccessful = false;
                    failureMessages.Add($"Member {recipient.MemberId}: {ex.Message}");

                    _logger.LogError(ex, 
                        "Exception during disbursement to member {MemberId}", 
                        recipient.MemberId);
                }
            }

            // Update final payout status
            if (allSuccessful)
            {
                await _payoutRepository.UpdatePayoutStatusAsync(
                    payoutId, PayoutStatus.Completed, null, cancellationToken);

                // Send notifications to all recipients
                await NotifyRecipientsAsync(payout, cancellationToken);

                _logger.LogInformation(
                    "Payout {PayoutId} completed successfully. All {Count} disbursements successful.",
                    payoutId, payout.Recipients.Count);

                return (true, null);
            }
            else
            {
                var errorMessage = $"Payout partially failed. {failureMessages.Count} disbursement(s) failed: " +
                                 string.Join("; ", failureMessages);

                await _payoutRepository.UpdatePayoutStatusAsync(
                    payoutId, PayoutStatus.Failed, errorMessage, cancellationToken);

                _logger.LogWarning(
                    "Payout {PayoutId} completed with failures: {ErrorMessage}",
                    payoutId, errorMessage);

                return (false, errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm payout {PayoutId}", payoutId);
            
            await _payoutRepository.UpdatePayoutStatusAsync(
                payoutId, PayoutStatus.Failed, ex.Message, cancellationToken);

            return (false, $"Failed to execute payout: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets payout history for a group
    /// </summary>
    public async Task<IEnumerable<Payout>> GetGroupPayoutHistoryAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return await _payoutRepository.GetGroupPayoutsAsync(groupId, cancellationToken);
    }

    /// <summary>
    /// Gets pending approvals for a group
    /// </summary>
    public async Task<IEnumerable<Payout>> GetPendingApprovalsAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return await _payoutRepository.GetPendingApprovalsAsync(groupId, cancellationToken);
    }

    #region Private Helper Methods

    /// <summary>
    /// Calculates rotating payout: one member receives principal only
    /// </summary>
    private async Task<List<PayoutRecipient>> CalculateRotatingPayoutAsync(
        StokvelsGroup group,
        Guid? specificRecipientId,
        CancellationToken cancellationToken)
    {
        // TODO: In production, query actual contributions from IContributionRepository
        // For MVP stub, calculate principal asBalance - AccruedInterest
        
        var recipientId = specificRecipientId ?? group.Members.First().MemberId;
        var recipient = group.Members.FirstOrDefault(m => m.MemberId == recipientId);
        
        if (recipient == null)
        {
            throw new ArgumentException("Recipient not found in group");
        }

        // Rotating payout: principal only (interest stays in group)
        // Stub calculation: assume equal contributions per member
        var principalPerMember = group.Balance.Amount / group.Members.Count;

        return new List<PayoutRecipient>
        {
            new PayoutRecipient
            {
                Id = Guid.NewGuid(),
                MemberId = recipient.MemberId,
                Amount = new Money(principalPerMember),
                BankAccountNumber = "STUB_BANK_ACCOUNT", // TODO: Get from Member entity
                CreatedAt = DateTime.UtcNow
            }
        };
    }

    /// <summary>
    /// Calculates year-end payout: distribute full balance proportionally to all members
    /// </summary>
    private async Task<List<PayoutRecipient>> CalculateYearEndPayoutAsync(
        StokvelsGroup group,
        CancellationToken cancellationToken)
    {
        // TODO: In production, calculate proportional shares based on actual contribution history
        // For MVP stub, distribute equally
        
        var memberCount = group.Members.Count;
        var amountPerMember = group.Balance.Amount / memberCount;

        var recipients = new List<PayoutRecipient>();

        foreach (var member in group.Members.Where(m => m.IsActive))
        {
            recipients.Add(new PayoutRecipient
            {
                Id = Guid.NewGuid(),
                MemberId = member.MemberId,
                Amount = new Money(amountPerMember),
                BankAccountNumber = "STUB_BANK_ACCOUNT", // TODO: Get from Member entity
                CreatedAt = DateTime.UtcNow
            });
        }

        return recipients;
    }

    /// <summary>
    /// Calculates partial withdrawal: requires 60% quorum approval
    /// </summary>
    private List<PayoutRecipient> CalculatePartialWithdrawalAsync(
        StokvelsGroup group,
        Guid recipientId,
        decimal amount)
    {
        var recipient = group.Members.FirstOrDefault(m => m.MemberId == recipientId);
        
        if (recipient == null)
        {
            throw new ArgumentException("Recipient not found in group");
        }

        // Validate withdrawal amount doesn't exceed member's share
        // Stub: allow up to 50% of group balance per member
        var maxWithdrawal = group.Balance.Amount * 0.5m;
        
        if (amount > maxWithdrawal)
        {
            throw new ArgumentException($"Withdrawal amount exceeds maximum allowed: R{maxWithdrawal:F2}");
        }

        return new List<PayoutRecipient>
        {
            new PayoutRecipient
            {
                Id = Guid.NewGuid(),
                MemberId = recipientId,
                Amount = new Money(amount),
                BankAccountNumber = "STUB_BANK_ACCOUNT", // TODO: Get from Member entity
                CreatedAt = DateTime.UtcNow
            }
        };
    }

    /// <summary>
    /// Executes EFT transfer via payment gateway
    /// </summary>
    private async Task<PaymentResult> ExecuteEftTransferAsync(
        Guid memberId,
        decimal amount,
        string description,
        CancellationToken cancellationToken)
    {
        // For MVP, use existing DeductFromAccountAsync as proxy for EFT
        // In production, would implement separate CreditToAccountAsync or EftTransferAsync
        
        // Stub: Log EFT and return success
        _logger.LogInformation(
            "STUB EFT: Transferring R{Amount} to member {MemberId} | Description: {Description}",
            amount, memberId, description);

        await Task.Delay(50, cancellationToken); // Simulate API call

        return new PaymentResult
        {
            Success = true,
            TransactionReference = $"EFT-{Guid.NewGuid():N}",
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Sends payout notifications to all recipients via SMS
    /// </summary>
    private async Task NotifyRecipientsAsync(Payout payout, CancellationToken cancellationToken)
    {
        foreach (var recipient in payout.Recipients.Where(r => r.IsSuccessful))
        {
            try
            {
                var phoneNumber = recipient.Member.Member.PhoneNumber;
                var language = recipient.Member.Member.PreferredLanguage ?? "en";

                await _smsNotificationService.SendPayoutNotificationSmsAsync(
                    phoneNumber,
                    payout.Group.Name,
                    recipient.Amount.Amount,
                    language,
                    cancellationToken);

                _logger.LogInformation(
                    "Sent payout notification to {PhoneNumber} for R{Amount}",
                    phoneNumber, recipient.Amount.Amount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to send payout notification to member {MemberId}",
                    recipient.MemberId);
            }
        }
    }

    #endregion
}
