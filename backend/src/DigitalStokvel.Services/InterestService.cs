using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Enums;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DigitalStokvel.Services;

/// <summary>
/// Service for calculating tiered interest with daily compounding
/// </summary>
public class InterestService : IInterestService
{
    private readonly IGroupRepository _groupRepository;
    private readonly ILogger<InterestService> _logger;

    // Interest rate tiers (annual rates)
    private const decimal Tier1Rate = 0.035m; // 3.5% for R0-R10K
    private const decimal Tier2Rate = 0.045m; // 4.5% for R10K-R50K
    private const decimal Tier3Rate = 0.055m; // 5.5% for R50K+

    // Tier balance thresholds
    private const decimal Tier2Threshold = 10000m;
    private const decimal Tier3Threshold = 50000m;

    public InterestService(
        IGroupRepository groupRepository,
        ILogger<InterestService> logger)
    {
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Calculate daily compound interest for a group
    /// Formula: A = P(1 + r/365)^days
    /// </summary>
    public async Task<InterestCalculation?> CalculateDailyInterestAsync(
        Guid groupId,
        DateTime calculationDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var group = await _groupRepository.GetByIdAsync(groupId);
            if (group == null)
            {
                _logger.LogWarning("Group {GroupId} not found for interest calculation", groupId);
                return null;
            }

            // Get principal amount (current balance)
            var principal = group.Balance.Amount;
            if (principal <= 0)
            {
                _logger.LogDebug("Group {GroupId} has zero balance, no interest to calculate", groupId);
                return null;
            }

            // Determine interest tier and rate
            var tier = DetermineInterestTier(principal);
            var annualRate = GetInterestRateForBalance(principal);

            // Calculate daily compound interest: A = P(1 + r/365)^days
            // For daily calculation, days = 1
            var dailyRate = annualRate / 365m;
            var interestAccrued = principal * dailyRate;

            // Round to 4 decimal places (standard for ZAR)
            interestAccrued = Math.Round(interestAccrued, 4);

            var calculation = new InterestCalculation
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                CalculationDate = calculationDate.Date,
                PrincipalAmount = new Money(principal),
                InterestRate = annualRate,
                AccruedAmount = new Money(interestAccrued),
                InterestTier = tier.ToString(),
                DaysCompounded = 1,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "InterestService"
            };

            _logger.LogInformation(
                "Interest calculated for group {GroupId}: Principal R{Principal}, Rate {Rate}%, Accrued R{Accrued}",
                groupId,
                principal,
                annualRate * 100,
                interestAccrued);

            return calculation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate interest for group {GroupId}", groupId);
            return null;
        }
    }

    /// <summary>
    /// Capitalize monthly interest: add AccruedInterest to Balance, reset AccruedInterest to 0
    /// </summary>
    public async Task<(bool Success, decimal NewBalance, string? ErrorMessage)> CapitalizeMonthlyAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var group = await _groupRepository.GetByIdAsync(groupId);
            if (group == null)
            {
                return (false, 0, "Group not found");
            }

            // In production: load AccruedInterest from a separate field or aggregate from InterestCalculation records
            // For stub implementation, assume we have AccruedInterest tracked somewhere
            _logger.LogWarning(
                "[STUB] CapitalizeMonthlyAsync for group {GroupId}. In production, implement proper AccruedInterest tracking and capitalization.",
                groupId);

            // Stub implementation - would update group.Balance and reset accrued interest
            var currentBalance = group.Balance.Amount;
            var accruedInterest = 0m; // Would fetch from tracking

            var newBalance = currentBalance + accruedInterest;

            _logger.LogInformation(
                "Monthly capitalization for group {GroupId}: Balance R{OldBalance} + Interest R{Interest} = R{NewBalance}",
                groupId,
                currentBalance,
                accruedInterest,
                newBalance);

            return (true, newBalance, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capitalize interest for group {GroupId}", groupId);
            return (false, 0, ex.Message);
        }
    }

    /// <summary>
    /// Get interest breakdown for a group for a date range
    /// </summary>
    public async Task<List<InterestCalculation>> GetInterestBreakdownAsync(
        Guid groupId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // In production: query InterestCalculation repository for date range
            _logger.LogWarning(
                "[STUB] GetInterestBreakdownAsync for group {GroupId} from {FromDate} to {ToDate}. Implement repository query.",
                groupId,
                fromDate,
                toDate);

            return await Task.FromResult(new List<InterestCalculation>());
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get interest breakdown for group {GroupId}",
                groupId);
            return new List<InterestCalculation>();
        }
    }

    /// <summary>
    /// Calculate year-to-date interest earnings
    /// </summary>
    public async Task<decimal> CalculateYearToDateEarningsAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var yearStart = new DateTime(DateTime.UtcNow.Year, 1, 1);
            var calculations = await GetInterestBreakdownAsync(
                groupId,
                yearStart,
                DateTime.UtcNow,
                cancellationToken);

            var ytdEarnings = calculations.Sum(c => c.AccruedAmount.Amount);

            _logger.LogInformation(
                "YTD earnings for group {GroupId}: R{Earnings}",
                groupId,
                ytdEarnings);

            return ytdEarnings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate YTD earnings for group {GroupId}", groupId);
            return 0;
        }
    }

    /// <summary>
    /// Determine interest tier based on balance
    /// </summary>
    public InterestTier DetermineInterestTier(decimal balance)
    {
        if (balance < Tier2Threshold)
            return InterestTier.Tier1_3_5Pct;
        else if (balance < Tier3Threshold)
            return InterestTier.Tier2_4_5Pct;
        else
            return InterestTier.Tier3_5_5Pct;
    }

    /// <summary>
    /// Get interest rate for a balance amount
    /// </summary>
    public decimal GetInterestRateForBalance(decimal balance)
    {
        var tier = DetermineInterestTier(balance);
        return tier switch
        {
            InterestTier.Tier1_3_5Pct => Tier1Rate,
            InterestTier.Tier2_4_5Pct => Tier2Rate,
            InterestTier.Tier3_5_5Pct => Tier3Rate,
            _ => Tier1Rate
        };
    }
}
