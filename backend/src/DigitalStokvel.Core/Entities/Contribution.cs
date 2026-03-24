using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Core.ValueObjects;
using DigitalStokvel.Core.Enums;

namespace DigitalStokvel.Core.Entities;

/// <summary>
/// Represents a financial contribution to a stokvel group
/// </summary>
public class Contribution : IAuditableEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Group receiving the contribution
    /// </summary>
    public Guid GroupId { get; set; }
    
    /// <summary>
    /// Member making the contribution
    /// </summary>
    public Guid MemberId { get; set; }
    
    /// <summary>
    /// Contribution amount with currency
    /// </summary>
    public Money Amount { get; set; } = new Money(0);
    
    /// <summary>
    /// Payment method used: OneTap, DebitOrder, USSD
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }
    
    /// <summary>
    /// Transaction status: Pending, Completed, Failed, Retrying
    /// </summary>
    public ContributionStatus Status { get; set; }
    
    /// <summary>
    /// Idempotency key to prevent duplicate transactions (24-hour TTL)
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when contribution was initiated
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Payment gateway transaction reference
    /// </summary>
    public string? PaymentGatewayReference { get; set; }
    
    /// <summary>
    /// Error message if transaction failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Retry count for failed debit orders (max 2 retries)
    /// </summary>
    public int RetryCount { get; set; } = 0;
    
    /// <summary>
    /// Next retry timestamp for failed debit orders (48-hour delay)
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    // Navigation properties
    public virtual StokvelsGroup? Group { get; set; }
    public virtual Member? Member { get; set; }

    // Audit properties (from IAuditableEntity)
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
