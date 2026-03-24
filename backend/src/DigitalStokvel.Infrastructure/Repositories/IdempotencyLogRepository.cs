using Microsoft.EntityFrameworkCore;
using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Infrastructure.Data;

namespace DigitalStokvel.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for idempotency log management
/// </summary>
public class IdempotencyLogRepository : IIdempotencyLogRepository
{
    private readonly ApplicationDbContext _context;

    public IdempotencyLogRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.Set<IdempotencyLog>()
            .AnyAsync(log => log.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<IdempotencyLog?> GetByKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.Set<IdempotencyLog>()
            .FirstOrDefaultAsync(log => log.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<IdempotencyLog> LogAsync(
        string idempotencyKey,
        Guid transactionId,
        string transactionType,
        string status,
        string? response = null,
        CancellationToken cancellationToken = default)
    {
        var log = new IdempotencyLog
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = idempotencyKey,
            TransactionId = transactionId,
            TransactionType = transactionType,
            Status = status,
            Response = response,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24) // 24-hour expiration
        };

        await _context.Set<IdempotencyLog>().AddAsync(log, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return log;
    }

    public async Task UpdateStatusAsync(
        string idempotencyKey,
        string status,
        string? response = null,
        CancellationToken cancellationToken = default)
    {
        var log = await GetByKeyAsync(idempotencyKey, cancellationToken);

        if (log != null)
        {
            log.Status = status;
            log.Response = response;

            if (status == "Completed" || status == "Failed")
            {
                log.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CleanupExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expiredLogs = await _context.Set<IdempotencyLog>()
            .Where(log => log.ExpiresAt < DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        _context.Set<IdempotencyLog>().RemoveRange(expiredLogs);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
