using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Enums;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Core.ValueObjects;
using DigitalStokvel.Infrastructure.Data;
using DigitalStokvel.Infrastructure.Notifications;

namespace DigitalStokvel.Services;

/// <summary>
/// Service for processing contributions with idempotency, transaction management, and retry logic
/// </summary>
public class ContributionService
{
    private readonly IContributionRepository _contributionRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ApplicationDbContext _dbContext;
    private readonly SmsNotificationService _smsNotificationService;
    private readonly ILogger<ContributionService> _logger;
    private readonly ResiliencePipeline _retryPipeline;

    // Retry constants
    private const int MaxRetryAttempts = 3;
    private const int DebitOrderMaxRetries = 2;
    private const int DebitOrderRetryDelayHours = 48;

    public ContributionService(
        IContributionRepository contributionRepository,
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        IPaymentGateway paymentGateway,
        ApplicationDbContext dbContext,
        SmsNotificationService smsNotificationService,
        ILogger<ContributionService> logger)
    {
        _contributionRepository = contributionRepository ?? throw new ArgumentNullException(nameof(contributionRepository));
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
        _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _smsNotificationService = smsNotificationService ?? throw new ArgumentNullException(nameof(smsNotificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure Polly retry pipeline (3 retries with exponential backoff)
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Payment gateway retry {AttemptNumber}/{MaxAttempts} after {Delay}ms delay. Exception: {Exception}",
                        args.AttemptNumber, MaxRetryAttempts, args.RetryDelay.TotalMilliseconds, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Processes a contribution with idempotency check, payment gateway integration, and transaction management
    /// </summary>
    public async Task<(bool Success, Contribution? Contribution, string? ErrorMessage)> ProcessContributionAsync(
        Guid memberId,
        Guid groupId,
        decimal amount,
        PaymentMethod paymentMethod,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Check idempotency - return cached result if already processed
            if (await _contributionRepository.IdempotencyKeyExistsAsync(idempotencyKey, cancellationToken))
            {
                var existingContribution = await _contributionRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
                
                _logger.LogInformation(
                    "Idempotent contribution request detected: {IdempotencyKey} - returning cached result",
                    idempotencyKey);

                return (existingContribution?.Status == ContributionStatus.Completed, existingContribution, null);
            }

            // 2. Validate member and group
            var member = await _memberRepository.GetByIdAsync(memberId, cancellationToken);
            if (member == null)
            {
                return (false, null, "Member not found");
            }

            var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
            if (group == null || !group.IsActive)
            {
                return (false, null, "Group not found or inactive");
            }

            // 3. Validate amount matches group's contribution amount
            if (amount != group.ContributionAmount.Amount)
            {
                return (false, null, $"Amount must be {group.ContributionAmount.Currency}{group.ContributionAmount.Amount}");
            }

            // 4. Create contribution record (Pending status)
            var contribution = new Contribution
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                MemberId = memberId,
                Amount = new Money(amount, "ZAR"),
                PaymentMethod = paymentMethod,
                Status = ContributionStatus.Pending,
                IdempotencyKey = idempotencyKey,
                Timestamp = DateTime.UtcNow,
                RetryCount = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = memberId.ToString()
            };

            // 5. Begin database transaction
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // 6. Persist contribution (Pending)
                await _contributionRepository.AddContributionAsync(contribution, cancellationToken);

                // 7. Process payment with retry policy
                PaymentResult paymentResult;
                
                try
                {
                    paymentResult = await _retryPipeline.ExecuteAsync(async ct => 
                        await _paymentGateway.DeductFromAccountAsync(
                            memberId, 
                            amount, 
                            "ZAR",
                            idempotencyKey, 
                            ct), 
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Payment gateway failed after {MaxRetries} retries for Contribution {ContributionId}",
                        MaxRetryAttempts, contribution.Id);

                    // Mark as failed or retrying (for debit orders)
                    contribution.Status = paymentMethod == PaymentMethod.DebitOrder 
                        ? ContributionStatus.Retrying 
                        : ContributionStatus.Failed;
                    contribution.ErrorMessage = ex.Message;
                    contribution.NextRetryAt = paymentMethod == PaymentMethod.DebitOrder
                        ? DateTime.UtcNow.AddHours(DebitOrderRetryDelayHours)
                        : null;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    // Schedule retry notification for debit orders
                    if (paymentMethod == PaymentMethod.DebitOrder)
                    {
                        await SendRetryNotificationAsync(member, group, contribution, cancellationToken);
                    }

                    return (false, contribution, "Payment failed. " + 
                        (paymentMethod == PaymentMethod.DebitOrder ? "Will retry in 48 hours." : "Please try again."));
                }

                if (!paymentResult.Success)
                {
                    // Payment failed - update contribution status
                    contribution.Status = paymentMethod == PaymentMethod.DebitOrder 
                        ? ContributionStatus.Retrying 
                        : ContributionStatus.Failed;
                    contribution.ErrorMessage = paymentResult.ErrorMessage;
                    contribution.PaymentGatewayReference = paymentResult.TransactionReference;
                    contribution.NextRetryAt = paymentMethod == PaymentMethod.DebitOrder
                        ? DateTime.UtcNow.AddHours(DebitOrderRetryDelayHours)
                        : null;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    // Send retry notification for debit orders
                    if (paymentMethod == PaymentMethod.DebitOrder && contribution.RetryCount < DebitOrderMaxRetries)
                    {
                        await SendRetryNotificationAsync(member, group, contribution, cancellationToken);
                    }

                    return (false, contribution, paymentResult.ErrorMessage ?? "Payment failed");
                }

                // 8. Payment successful - update contribution and group balance
                contribution.Status = ContributionStatus.Completed;
                contribution.PaymentGatewayReference = paymentResult.TransactionReference;
                contribution.ModifiedAt = DateTime.UtcNow;

                // Update group balance
                group.Balance = new Money(group.Balance.Amount + amount, "ZAR");
                group.ModifiedAt = DateTime.UtcNow;
                group.ModifiedBy = memberId.ToString();

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Contribution completed: {ContributionId} | Member: {MemberId} | Group: {GroupId} | Amount: R{Amount}",
                    contribution.Id, memberId, groupId, amount);

                // 9. Send confirmation notification
                await _smsNotificationService.SendContributionConfirmationSmsAsync(
                    member.PhoneNumber,
                    group.Name,
                    amount,
                    group.Balance.Amount,
                    member.PreferredLanguage,
                    cancellationToken);

                return (true, contribution, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Transaction failed for contribution {IdempotencyKey}", idempotencyKey);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing contribution for Member {MemberId}, Group {GroupId}", memberId, groupId);
            return (false, null, "An error occurred while processing your contribution. Please try again.");
        }
    }

    /// <summary>
    /// Retries failed debit order contributions (background job)
    /// </summary>
    public async Task<int> RetryFailedDebitOrdersAsync(CancellationToken cancellationToken = default)
    {
        var pendingRetries = await _contributionRepository.GetPendingRetryContributionsAsync(cancellationToken);
        var retryCount = 0;

        foreach (var contribution in pendingRetries)
        {
            if (contribution.RetryCount >= DebitOrderMaxRetries)
            {
                // Max retries reached - mark as permanently failed
                contribution.Status = ContributionStatus.Failed;
                contribution.ErrorMessage = "Maximum retry attempts exceeded";
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Debit order retry limit reached: Contribution {ContributionId} | Retries: {RetryCount}",
                    contribution.Id, contribution.RetryCount);
                continue;
            }

            // Attempt retry
            contribution.RetryCount++;

            var paymentResult = await _paymentGateway.DeductFromAccountAsync(
                contribution.MemberId,
                contribution.Amount.Amount,
                contribution.Amount.Currency,
                contribution.IdempotencyKey,
                cancellationToken);

            if (paymentResult.Success)
            {
                // Retry successful
                contribution.Status = ContributionStatus.Completed;
                contribution.PaymentGatewayReference = paymentResult.TransactionReference;

                // Update group balance
                var group = await _groupRepository.GetByIdAsync(contribution.GroupId, cancellationToken);
                if (group != null)
                {
                    group.Balance = new Money(group.Balance.Amount + contribution.Amount.Amount, "ZAR");
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Debit order retry successful: Contribution {ContributionId} | Attempt {RetryCount}",
                    contribution.Id, contribution.RetryCount);

                // Send success notification
                if (contribution.Member != null && group != null)
                {
                    await _smsNotificationService.SendContributionConfirmationSmsAsync(
                        contribution.Member.PhoneNumber,
                        group.Name,
                        contribution.Amount.Amount,
                        group.Balance.Amount,
                        contribution.Member.PreferredLanguage,
                        cancellationToken);
                }

                retryCount++;
            }
            else
            {
                // Retry failed - schedule next attempt or mark as failed
                if (contribution.RetryCount < DebitOrderMaxRetries)
                {
                    contribution.Status = ContributionStatus.Retrying;
                    contribution.NextRetryAt = DateTime.UtcNow.AddHours(DebitOrderRetryDelayHours);
                    contribution.ErrorMessage = paymentResult.ErrorMessage;

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    // Send retry notification
                    if (contribution.Member != null && contribution.Group != null)
                    {
                        await SendRetryNotificationAsync(contribution.Member, contribution.Group, contribution, cancellationToken);
                    }
                }
                else
                {
                    contribution.Status = ContributionStatus.Failed;
                    contribution.ErrorMessage = "Maximum retry attempts exceeded";
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                _logger.LogWarning(
                    "Debit order retry failed: Contribution {ContributionId} | Attempt {RetryCount} | Error: {Error}",
                    contribution.Id, contribution.RetryCount, paymentResult.ErrorMessage);
            }
        }

        return retryCount;
    }

    #region Private Helper Methods

    /// <summary>
    /// Sends retry notification for failed debit order
    /// </summary>
    private async Task SendRetryNotificationAsync(
        Member member,
        StokvelsGroup group,
        Contribution contribution,
        CancellationToken cancellationToken)
    {
        var retryMessage = contribution.RetryCount < DebitOrderMaxRetries
            ? $"Your debit order for {group.Name} didn't go through. We'll try again in 48 hours. Please ensure funds are available."
            : $"Your debit order for {group.Name} has failed after multiple attempts. Please contact your Chairperson.";

        _logger.LogInformation(
            "Sending retry notification: Member {MemberId} | Contribution {ContributionId} | Retry {RetryCount}",
            member.Id, contribution.Id, contribution.RetryCount);

        // In production, send actual SMS via Azure Communication Services
        await Task.CompletedTask;
    }

    #endregion
}
