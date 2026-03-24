using DigitalStokvel.Core.Enums;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Core.ValueObjects;

namespace DigitalStokvel.Core.Entities;

/// <summary>
/// Represents a payout transaction from a group to one or more members
/// </summary>
public class Payout : IAuditableEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Group initiating the payout
    /// </summary>
    public Guid GroupId { get; set; }
    public StokvelsGroup Group { get; set; } = null!;
    
    /// <summary>
    /// Type of payout (Rotating, YearEnd, PartialWithdrawal)
    /// </summary>
    public PayoutType PayoutType { get; set; }
    
    /// <summary>
    /// Total amount being disbursed across all recipients
    /// </summary>
    public Money TotalAmount { get; set; } = new Money(0);
    
    /// <summary>
    /// Member who initiated the payout (must be Chairperson)
    /// </summary>
    public Guid InitiatedBy { get; set; }
    public GroupMember InitiatedByMember { get; set; } = null!;
    
    /// <summary>
    /// Member who confirmed the payout (must be Treasurer)
    /// </summary>
    public Guid? ConfirmedBy { get; set; }
    public GroupMember? ConfirmedByMember { get; set; }
    
    /// <summary>
    /// Current status of the payout workflow
    /// </summary>
    public PayoutStatus Status { get; set; } = PayoutStatus.PendingTreasurerApproval;
    
    /// <summary>
    /// Scheduled time for payout execution (defaults to immediate)
    /// </summary>
    public DateTime? ExecutionTime { get; set; }
    
    /// <summary>
    /// Actual time when payout was completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Reason for payout (e.g., "Monthly rotating payout - March 2024")
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// Error message if payout failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Recipients of this payout
    /// </summary>
    public ICollection<PayoutRecipient> Recipients { get; set; } = new List<PayoutRecipient>();
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
