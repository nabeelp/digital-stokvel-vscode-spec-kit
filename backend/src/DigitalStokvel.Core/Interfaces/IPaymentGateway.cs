namespace DigitalStokvel.Core.Interfaces;

/// <summary>
/// Payment gateway interface for deducting funds from member accounts
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Deducts the specified amount from a member's bank account
    /// </summary>
    /// <param name="memberId">Member identifier</param>
    /// <param name="amount">Amount to deduct (in decimal)</param>
    /// <param name="currency">Currency code (default ZAR)</param>
    /// <param name="idempotencyKey">Idempotency key to prevent duplicate transactions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment result with transaction reference and status</returns>
    Task<PaymentResult> DeductFromAccountAsync(
        Guid memberId,
        decimal amount,
        string currency = "ZAR",
        string? idempotencyKey = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets up a recurring debit order for a member
    /// </summary>
    /// <param name="memberId">Member identifier</param>
    /// <param name="amount">Amount to deduct per cycle</param>
    /// <param name="frequency">Debit order frequency (Monthly, Biweekly, Weekly)</param>
    /// <param name="startDate">First deduction date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Debit order reference</returns>
    Task<DebitOrderResult> SetupDebitOrderAsync(
        Guid memberId,
        decimal amount,
        string frequency,
        DateTime startDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an active debit order
    /// </summary>
    Task<bool> CancelDebitOrderAsync(string debitOrderReference, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a payment transaction
/// </summary>
public record PaymentResult
{
    public bool Success { get; init; }
    public string? TransactionReference { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Result of debit order setup
/// </summary>
public record DebitOrderResult
{
    public bool Success { get; init; }
    public string? DebitOrderReference { get; init; }
    public DateTime? NextDebitDate { get; init; }
    public string? ErrorMessage { get; init; }
}
