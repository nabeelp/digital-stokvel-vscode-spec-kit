namespace DigitalStokvel.Core.Enums;

/// <summary>
/// Status of a contribution transaction
/// </summary>
public enum ContributionStatus
{
    /// <summary>
    /// Contribution is pending payment gateway processing
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Contribution completed successfully and funds transferred
    /// </summary>
    Completed = 1,
    
    /// <summary>
    /// Contribution failed due to payment gateway error or insufficient funds
    /// </summary>
    Failed = 2,
    
    /// <summary>
    /// Contribution is being retried after initial failure (debit order retry)
    /// </summary>
    Retrying = 3
}
