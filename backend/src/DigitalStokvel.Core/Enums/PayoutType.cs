namespace DigitalStokvel.Core.Enums;

/// <summary>
/// Type of payout distribution
/// </summary>
public enum PayoutType
{
    /// <summary>
    /// Rotating payout: One member receives principal only (sum of their contributions)
    /// Interest remains in group wallet
    /// </summary>
    RotatingCycle = 1,
    
    /// <summary>
    /// Year-end pot: Full balance (principal + interest) distributed proportionally to all members
    /// Group dissolves or resets after this payout
    /// </summary>
    YearEndPot = 2,
    
    /// <summary>
    /// Partial withdrawal: Member-initiated withdrawal requiring 60% quorum vote
    /// Amount can be any value up to member's share
    /// </summary>
    PartialWithdrawal = 3
}
