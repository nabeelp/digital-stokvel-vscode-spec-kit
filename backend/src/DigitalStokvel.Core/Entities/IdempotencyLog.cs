namespace DigitalStokvel.Core.Entities;

/// <summary>
/// Tracks idempotency keys to prevent duplicate transaction processing
/// </summary>
public class IdempotencyLog
{
    public Guid Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid TransactionId { get; set; }
    public string TransactionType { get; set; } = string.Empty; // "Contribution", "Payout", etc.
    public string Status { get; set; } = string.Empty; // "Pending", "Completed", "Failed"
    public string? Response { get; set; } // JSON response stored for retrieval
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime ExpiresAt { get; set; } // Auto-cleanup after 24 hours
}
