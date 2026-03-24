using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Enums;

namespace DigitalStokvel.Core.Interfaces;

/// <summary>
/// Service for calculating and managing interest on group savings
/// </summary>
public interface IInterestService
{
    /// <summary>
    /// Calculate daily compound interest for a group
    /// Formula: A = P(1 + r/365)^days
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="calculationDate">Date of calculation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Interest calculation record with accrued amount</returns>
    Task<InterestCalculation?> CalculateDailyInterestAsync(
        Guid groupId,
        DateTime calculationDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Capitalize monthly interest: add AccruedInterest to Balance, reset AccruedInterest to 0
    /// Runs on the 1st of each month
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status and new balance after capitalization</returns>
    Task<(bool Success, decimal NewBalance, string? ErrorMessage)> CapitalizeMonthlyAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get interest breakdown for a group (YTD earnings, daily calculations)
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="fromDate">Start date for breakdown</param>
    /// <param name="toDate">End date for breakdown</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of interest calculations for the period</returns>
    Task<List<InterestCalculation>> GetInterestBreakdownAsync(
        Guid groupId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate year-to-date interest earnings for a group
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total YTD interest earned</returns>
    Task<decimal> CalculateYearToDateEarningsAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determine the current interest tier for a group based on balance
    /// </summary>
    /// <param name="balance">Current group balance</param>
    /// <returns>Interest tier (Tier1_3_5Pct, Tier2_4_5Pct, Tier3_5_5Pct)</returns>
    InterestTier DetermineInterestTier(decimal balance);

    /// <summary>
    /// Get the annual interest rate for a specific balance amount
    /// </summary>
    /// <param name="balance">Balance amount</param>
    /// <returns>Annual interest rate as decimal (e.g., 0.035 for 3.5%)</returns>
    decimal GetInterestRateForBalance(decimal balance);
}
