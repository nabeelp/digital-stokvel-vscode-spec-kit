using DigitalStokvel.Core.Entities;

namespace DigitalStokvel.Core.Interfaces;

/// <summary>
/// Repository for managing idempotency logs to prevent duplicate transactions
/// </summary>
public interface IIdempotencyLogRepository
{
    /// <summary>
    /// Checks if an idempotency key already exists
    /// </summary>
    Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the stored result for an idempotency key
    /// </summary>
    Task<IdempotencyLog?> GetByKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a new idempotency key with transaction details
    /// </summary>
    Task<IdempotencyLog> LogAsync(
        string idempotencyKey,
        Guid transactionId,
        string transactionType,
        string status,
        string? response = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status and response of an idempotency log
    /// </summary>
    Task UpdateStatusAsync(
        string idempotencyKey,
        string status,
        string? response = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes expired idempotency logs (older than 24 hours)
    /// </summary>
    Task CleanupExpiredAsync(CancellationToken cancellationToken = default);
}
