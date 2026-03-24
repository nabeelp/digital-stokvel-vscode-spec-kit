using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Core.ValueObjects;

namespace DigitalStokvel.Core.Entities;

/// <summary>
/// Represents a single recipient in a payout, tracking their disbursement
/// </summary>
public class PayoutRecipient : IAuditableEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Parent payout transaction
    /// </summary>
    public Guid PayoutId { get; set; }
    public Payout Payout { get; set; } = null!;
    
    /// <summary>
    /// Member receiving the funds
    /// </summary>
    public Guid MemberId { get; set; }
    public GroupMember Member { get; set; } = null!;
    
    /// <summary>
    /// Amount being disbursed to this member
    /// </summary>
    public Money Amount { get; set; } = new Money(0);
    
    /// <summary>
    /// Bank's EFT reference number
    /// </summary>
    public string? EftReference { get; set; }
    
    /// <summary>
    /// Member's bank account details (for audit trail)
    /// </summary>
    public string? BankAccountNumber { get; set; }
    
    /// <summary>
    /// Time when funds were successfully disbursed
    /// </summary>
    public DateTime? DisbursedAt { get; set; }
    
    /// <summary>
    /// Whether disbursement was successful
    /// </summary>
    public bool IsSuccessful { get; set; }
    
    /// <summary>
    /// Error message if disbursement failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Number of retry attempts for failed disbursements
    /// </summary>
    public int RetryCount { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
