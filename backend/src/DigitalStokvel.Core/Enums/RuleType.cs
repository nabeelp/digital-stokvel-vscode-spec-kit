namespace DigitalStokvel.Core.Enums;

/// <summary>
/// Types of governance rules that can be defined in group constitution
/// </summary>
public enum RuleType
{
    /// <summary>
    /// Penalty amount or percentage for missed contributions (e.g., R50 or 10%)
    /// </summary>
    MissedPaymentPenalty = 1,
    
    /// <summary>
    /// Days allowed after due date before penalty applies (e.g., 3 days)
    /// </summary>
    GracePeriod = 2,
    
    /// <summary>
    /// Criteria for removing inactive/non-contributing members
    /// (e.g., 3 consecutive missed payments)
    /// </summary>
    MemberRemovalCriteria = 3,
    
    /// <summary>
    /// Percentage of members required for vote approval (default: 60%)
    /// </summary>
    QuorumThreshold = 4,
    
    /// <summary>
    /// Early withdrawal policy and penalty structure
    /// </summary>
    EarlyWithdrawalPolicy = 5,
    
    /// <summary>
    /// Interest distribution policy (equal split vs proportional)
    /// </summary>
    InterestDistributionPolicy = 6,
    
    /// <summary>
    /// Maximum number of members allowed in the group
    /// </summary>
    MaxMemberLimit = 7
}
