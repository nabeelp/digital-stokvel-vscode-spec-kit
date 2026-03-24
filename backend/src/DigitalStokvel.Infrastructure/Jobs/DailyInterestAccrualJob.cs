using DigitalStokvel.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DigitalStokvel.Infrastructure.Jobs;

/// <summary>
/// Background job that runs daily at 00:01 UTC
/// Calculates and accrues daily compound interest for all active groups
/// </summary>
public class DailyInterestAccrualJob
{
    private readonly ILogger<DailyInterestAccrualJob> _logger;
    private readonly IGroupRepository _groupRepository;
    private readonly IInterestService _interestService;

    public DailyInterestAccrualJob(
        ILogger<DailyInterestAccrualJob> logger,
        IGroupRepository groupRepository,
        IInterestService interestService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        _interestService = interestService ?? throw new ArgumentNullException(nameof(interestService));
    }

    /// <summary>
    /// Execute daily interest accrual for all active groups with positive balances
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var calculationDate = DateTime.UtcNow.Date;
        
        _logger.LogInformation(
            "Daily interest accrual job started at {Timestamp} for date {CalculationDate}",
            DateTime.UtcNow,
            calculationDate);

        try
        {
            // Get all active groups with positive balances
            // In production, implement GetGroupsWithPositiveBalancesAsync repository method
            _logger.LogWarning(
                "[STUB] DailyInterestAccrualJob - would iterate through all active groups with balance > 0. Implement repository method.");

            int calculationsCompleted = 0;
            int calculationsFailed = 0;
            decimal totalInterestAccrued = 0;

            // Stub implementation - would iterate through actual groups
            var activeGroups = new List<Guid>(); // Would come from repository

            foreach (var groupId in activeGroups)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var calculation = await _interestService.CalculateDailyInterestAsync(
                        groupId,
                        calculationDate,
                        cancellationToken);

                    if (calculation != null)
                    {
                        // In production: save calculation to InterestCalculation table
                        // and update group's AccruedInterest field
                        calculationsCompleted++;
                        totalInterestAccrued += calculation.AccruedAmount.Amount;

                        _logger.LogDebug(
                            "Interest accrued for group {GroupId}: R{Amount} at {Rate}% rate",
                            groupId,
                            calculation.AccruedAmount.Amount,
                            calculation.InterestRate * 100);
                    }
                    else
                    {
                        _logger.LogDebug(
                            "No interest calculation for group {GroupId} (zero balance or error)",
                            groupId);
                    }

                    // Rate limiting between calculations
                    await Task.Delay(50, cancellationToken);
                }
                catch (Exception ex)
                {
                    calculationsFailed++;
                    _logger.LogError(
                        ex,
                        "Exception while calculating interest for group {GroupId}",
                        groupId);
                }
            }

            _logger.LogInformation(
                "Daily interest accrual completed for {Date}. Calculations: {Completed}, Failed: {Failed}, Total Accrued: R{Total}",
                calculationDate,
                calculationsCompleted,
                calculationsFailed,
                totalInterestAccrued);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Daily interest accrual job failed");
            throw;
        }
    }
}
