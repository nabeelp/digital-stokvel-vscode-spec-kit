namespace DigitalStokvel.Core.Enums;

/// <summary>
/// Status of payout workflow
/// </summary>
public enum PayoutStatus
{
    /// <summary>
    /// Payout initiated by Chairperson, awaiting Treasurer confirmation
    /// </summary>
    PendingTreasurerApproval = 1,
    
    /// <summary>
    /// Partial withdrawal: awaiting 60% member quorum vote
    /// </summary>
    PendingQuorum = 2,
    
    /// <summary>
    /// Payout approved by Treasurer or quorum, ready for execution
    /// </summary>
    Approved = 3,
    
    /// <summary>
    /// EFT disbursements in progress
    /// </summary>
    InProgress = 4,
    
    /// <summary>
    /// All disbursements completed successfully
    /// </summary>
    Completed = 5,
    
    /// <summary>
    /// Payout failed (EFT errors, insufficient balance, etc.)
    /// </summary>
    Failed = 6,
    
    /// <summary>
    /// Payout cancelled by Chairperson or Treasurer before execution
    /// </summary>
    Cancelled = 7
}
