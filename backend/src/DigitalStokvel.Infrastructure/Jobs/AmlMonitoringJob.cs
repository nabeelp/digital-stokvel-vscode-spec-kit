using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigitalStokvel.Infrastructure.Data;
using DigitalStokvel.Core.Enums;
using DigitalStokvel.Core.ValueObjects;

namespace DigitalStokvel.Infrastructure.Jobs;

/// <summary>
/// Background job for Anti-Money Laundering (AML) monitoring
/// Flags suspicious transactions per FICA requirements
/// Runs daily to check for threshold breaches
/// </summary>
public class AmlMonitoringJob
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AmlMonitoringJob> _logger;

    // AML thresholds per FICA regulations
    private static readonly Money SINGLE_TRANSACTION_THRESHOLD = new Money(20000m); // R20,000
    private static readonly Money MONTHLY_INFLOW_THRESHOLD = new Money(100000m);    // R100,000

    public AmlMonitoringJob(
        ApplicationDbContext context,
        ILogger<AmlMonitoringJob> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute AML monitoring checks
    /// Returns: (TotalChecked, FlaggedTransactions, FlaggedGroups)
    /// </summary>
    public async Task<(int TotalChecked, int FlaggedTransactions, int FlaggedGroups)> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting AML monitoring job at {Time}", DateTime.UtcNow);

        var yesterday = DateTime.UtcNow.AddDays(-1);
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        int totalChecked = 0;
        int flaggedTransactions = 0;
        int flaggedGroups = 0;

        // Check 1: Single large transactions > R20K
        var largeTransactions = await _context.Contributions
            .Where(c => c.Status == ContributionStatus.Completed &&
                       c.Timestamp >= yesterday &&
                       c.Amount > SINGLE_TRANSACTION_THRESHOLD)
            .Include(c => c.Member)
            .Include(c => c.Group)
            .ToListAsync(cancellationToken);

        totalChecked += largeTransactions.Count;
        flaggedTransactions += largeTransactions.Count;

        foreach (var transaction in largeTransactions)
        {
            _logger.LogWarning(
                "AML ALERT: Large transaction detected - Amount: R{Amount:N2}, Member: {MemberId}, " +
                "Group: {GroupId}, Transaction: {ContributionId}",
                transaction.Amount,
                transaction.MemberId,
                transaction.GroupId,
                transaction.Id);

            await CreateAuditLogAsync(
                entityType: "Contribution",
                entityId: transaction.Id,
                action: "AML_FLAG_LARGE_TRANSACTION",
                reason: $"Single transaction exceeds R{SINGLE_TRANSACTION_THRESHOLD:N0} threshold",
                memberId: transaction.MemberId,
                cancellationToken: cancellationToken);
        }

        // Check 2: Monthly inflows > R100K per group
        var groupsWithHighInflows = await _context.Contributions
            .Where(c => c.Status == ContributionStatus.Completed &&
                       c.Timestamp >= startOfMonth)
            .GroupBy(c => c.GroupId)
            .Select(g => new
            {
                GroupId = g.Key,
                TotalInflow = g.Sum(c => c.Amount.Amount) // Sum the decimal Amount property
            })
            .Where(g => g.TotalInflow > MONTHLY_INFLOW_THRESHOLD.Amount)
            .ToListAsync(cancellationToken);

        totalChecked += groupsWithHighInflows.Count;
        flaggedGroups += groupsWithHighInflows.Count;

        foreach (var group in groupsWithHighInflows)
        {
            var groupDetails = await _context.StokvelsGroups
                .FirstOrDefaultAsync(g => g.Id == group.GroupId, cancellationToken);

            _logger.LogWarning(
                "AML ALERT: High monthly inflow detected - Group: {GroupId} ({GroupName}), " +
                "Monthly Total: R{Amount:N2}",
                group.GroupId,
                groupDetails?.Name ?? "Unknown",
                group.TotalInflow);

            await CreateAuditLogAsync(
                entityType: "StokvelsGroup",
                entityId: group.GroupId,
                action: "AML_FLAG_HIGH_MONTHLY_INFLOW",
                reason: $"Monthly inflow exceeds R{MONTHLY_INFLOW_THRESHOLD:N0} threshold",
                memberId: null,
                cancellationToken: cancellationToken);

            // Additional alert for bank compliance team
            // In production, this would send an email or notification to compliance officers
            _logger.LogInformation(
                "TODO: Notify compliance team about group {GroupId} for manual review", 
                group.GroupId);
        }

        // Check 3: Rapid successive deposits (potential structuring)
        var rapidDeposits = await DetectStructuringPatternsAsync(cancellationToken);
        flaggedTransactions += rapidDeposits;

        _logger.LogInformation(
            "AML monitoring completed. Checked: {TotalChecked}, " +
            "Flagged Transactions: {FlaggedTransactions}, Flagged Groups: {FlaggedGroups}",
            totalChecked, flaggedTransactions, flaggedGroups);

        return (totalChecked, flaggedTransactions, flaggedGroups);
    }

    /// <summary>
    /// Detect potential structuring: multiple transactions just below threshold
    /// within short time period (possible attempt to evade detection)
    /// </summary>
    private async Task<int> DetectStructuringPatternsAsync(CancellationToken cancellationToken)
    {
        var lookbackHours = 24;
        var lookbackTime = DateTime.UtcNow.AddHours(-lookbackHours);
        var structuringThreshold = SINGLE_TRANSACTION_THRESHOLD.Amount * 0.9m; // 90% of threshold

        var flaggedCount = 0;

        // Group transactions by member in last 24 hours
        var memberTransactions = await _context.Contributions
            .Where(c => c.Status == ContributionStatus.Completed &&
                       c.Timestamp >= lookbackTime &&
                       c.Amount.Amount > structuringThreshold &&
                       c.Amount.Amount < SINGLE_TRANSACTION_THRESHOLD.Amount)
            .GroupBy(c => c.MemberId)
            .Select(g => new
            {
                MemberId = g.Key,
                Transactions = g.Select(c => new { c.Id, c.Amount.Amount, c.Timestamp }).ToList()
            })
            .Where(g => g.Transactions.Count >= 3) // 3 or more near-threshold transactions
            .ToListAsync(cancellationToken);

        foreach (var member in memberTransactions)
        {
            var totalAmount = member.Transactions.Sum(t => t.Amount);

            _logger.LogWarning(
                "AML ALERT: Potential structuring detected - Member: {MemberId}, " +
                "Transactions: {Count}, Total: R{Total:N2} in {Hours}h",
                member.MemberId,
                member.Transactions.Count,
                totalAmount,
                lookbackHours);

            // Log each transaction in the pattern
            foreach (var transaction in member.Transactions)
            {
                await CreateAuditLogAsync(
                    entityType: "Contribution",
                    entityId: transaction.Id,
                    action: "AML_FLAG_POTENTIAL_STRUCTURING",
                    reason: $"Part of {member.Transactions.Count} near-threshold transactions totaling R{totalAmount:N2}",
                    memberId: member.MemberId,
                    cancellationToken: cancellationToken);

                flaggedCount++;
            }
        }

        return flaggedCount;
    }

    /// <summary>
    /// Create an audit log entry for AML flagging
    /// </summary>
    private async Task CreateAuditLogAsync(
        string entityType,
        Guid entityId,
        string action,
        string reason,
        Guid? memberId,
        CancellationToken cancellationToken)
    {
        var auditLog = new Core.Entities.AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = "SYSTEM_AML_JOB",
            MemberId = memberId,
            Timestamp = DateTime.UtcNow,
            Reason = reason,
            HttpMethod = "BACKGROUND_JOB",
            RequestPath = "/jobs/aml-monitoring"
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
