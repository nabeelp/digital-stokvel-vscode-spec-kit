using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Enums;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DigitalStokvel.Infrastructure.Jobs;

/// <summary>
/// Background job to monitor missed payments and apply escalation policies
/// Covers T153 (missed payment escalation) and T154 (late fee application)
/// </summary>
public class MissedPaymentEscalationJob
{
    private readonly ApplicationDbContext _context;
    private readonly ISmsNotificationService _smsNotificationService;
    private readonly ILogger<MissedPaymentEscalationJob> _logger;

    public MissedPaymentEscalationJob(
        ApplicationDbContext context,
        ISmsNotificationService smsNotificationService,
        ILogger<MissedPaymentEscalationJob> logger)
    {
        _context = context;
        _smsNotificationService = smsNotificationService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the missed payment escalation workflow
    /// Runs daily via Hangfire
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting missed payment escalation job");

        try
        {
            // Get all active groups
            var groups = await _context.StokvelsGroups
                .Where(g => g.IsActive)
                .ToListAsync(cancellationToken);

            var totalProcessed = 0;
            var totalNotified = 0;
            var totalPenaltiesApplied = 0;

            foreach (var group in groups)
            {
                try
                {
                    var (processed, notified, penaltiesApplied) = await ProcessGroupMissedPaymentsAsync(
                        group, cancellationToken);
                        
                    totalProcessed += processed;
                    totalNotified += notified;
                    totalPenaltiesApplied += penaltiesApplied;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process missed payments for group {GroupId}", group.Id);
                }
            }

            _logger.LogInformation(
                "Missed payment escalation job completed. Processed: {Processed}, Notified: {Notified}, Penalties: {Penalties}",
                totalProcessed, totalNotified, totalPenaltiesApplied);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute missed payment escalation job");
            throw;
        }
    }

    private async Task<(int Processed, int Notified, int PenaltiesApplied)>  ProcessGroupMissedPaymentsAsync(
        StokvelsGroup group,
        CancellationToken cancellationToken)
    {
        int processed = 0;
        int notified = 0;
        int penaltiesApplied = 0;

        // Get governance rules for grace period and penalty
        var gracePeriodRule = await _context.GovernanceRules
            .FirstOrDefaultAsync(r => r.GroupId == group.Id && 
                                    r.RuleType == RuleType.GracePeriod && 
                                    r.IsActive, cancellationToken);

        var penaltyRule = await _context.GovernanceRules
            .FirstOrDefaultAsync(r => r.GroupId == group.Id && 
                                    r.RuleType == RuleType.MissedPaymentPenalty && 
                                    r.IsActive, cancellationToken);

        // Parse grace period (default: 3 days)
        int gracePeriodDays = 3;
        if (gracePeriodRule != null)
        {
            try
            {
                var ruleData = JsonDocument.Parse(gracePeriodRule.RuleValue);
                if (ruleData.RootElement.TryGetProperty("days", out var daysElement))
                {
                    gracePeriodDays = daysElement.GetInt32();
                }
            }
            catch
            {
                _logger.LogWarning("Failed to parse grace period for group {GroupId}, using default 3 days", group.Id);
            }
        }

        // Calculate contribution due date based on frequency
        var dueDate = CalculateDueDate(group, DateTime.UtcNow);
        var gracePeriodEnd = dueDate.AddDays(gracePeriodDays);

        // Get all active members
        var members = await _context.GroupMembers
            .Include(gm => gm.Member)
            .Where(gm => gm.GroupId == group.Id && gm.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var member in members)
        {
            processed++;

            // Check if member has paid for current cycle
            var hasPaid = await _context.Contributions
                .AnyAsync(c => c.GroupId == group.Id &&
                             c.MemberId == member.MemberId &&
                             c.Status == ContributionStatus.Completed &&
                             c.Timestamp >= dueDate.AddMonths(-1) && // Look back one cycle
                             c.Timestamp <= dueDate,
                             cancellationToken);

            if (!hasPaid)
            {
                var daysPastDue = (DateTime.UtcNow - dueDate).TotalDays;

                if (daysPastDue > 0 && daysPastDue <= gracePeriodDays)
                {
                    // Within grace period - send reminder
                    await SendGracePeriodReminderAsync(group, member, gracePeriodEnd, cancellationToken);
                    notified++;
                }
                else if (daysPastDue > gracePeriodDays)
                {
                    // Grace period expired - apply penalty and notify Chairperson
                    if (penaltyRule != null)
                    {
                        await ApplyLateFeeAsync(group, member, penaltyRule, cancellationToken);
                        penaltiesApplied++;
                    }

                    await NotifyChairpersonOfMissedPaymentAsync(group, member, (int)daysPastDue, cancellationToken);
                    notified++;
                }
            }
        }

        return (processed, notified, penaltiesApplied);
    }

    private DateTime CalculateDueDate(StokvelsGroup group, DateTime currentDate)
    {
        // Stub: Calculate based on contribution frequency
        // In production, would use actual group creation date and cycle tracking
        return group.ContributionFrequency switch
        {
            "Weekly" => currentDate.Date.AddDays(-(int)currentDate.DayOfWeek).AddDays(1), // Monday
            "Biweekly" => currentDate.Date.AddDays(-(int)currentDate.DayOfWeek).AddDays(1).AddDays(-7), // Last Monday
            "Monthly" => new DateTime(currentDate.Year, currentDate.Month, 1), // 1st of month
            _ => currentDate.Date
        };
    }

    private async Task SendGracePeriodReminderAsync(
        StokvelsGroup group,
        GroupMember member,
        DateTime gracePeriodEnd,
        CancellationToken cancellationToken)
    {
        try
        {
            var phoneNumber = member.Member.PhoneNumber;
            var language = member.Member.PreferredLanguage ?? "en";
            var daysRemaining = (int)Math.Ceiling((gracePeriodEnd - DateTime.UtcNow).TotalDays);

            _logger.LogInformation(
                "STUB: Send grace period reminder to {PhoneNumber} for group {GroupName}. Days remaining: {DaysRemaining}",
                phoneNumber, group.Name, daysRemaining);

            // In production, would send actual SMS
            // await _smsNotificationService.SendPaymentReminderSmsAsync(...)

            await Task.Delay(10, cancellationToken); // Simulate notification
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send grace period reminder to member {MemberId}", member.MemberId);
        }
    }

    private async Task ApplyLateFeeAsync(
        StokvelsGroup group,
        GroupMember member,
        GovernanceRule penaltyRule,
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse penalty rule
            decimal penaltyAmount = 0;
            try
            {
                var ruleData = JsonDocument.Parse(penaltyRule.RuleValue);
                
                if (ruleData.RootElement.TryGetProperty("type", out var typeElement))
                {
                    var penaltyType = typeElement.GetString();
                    
                    if (penaltyType == "fixed" && ruleData.RootElement.TryGetProperty("amount", out var amountElement))
                    {
                        penaltyAmount = amountElement.GetDecimal();
                    }
                    else if (penaltyType == "percentage" && ruleData.RootElement.TryGetProperty("percentage", out var percentageElement))
                    {
                        penaltyAmount = group.ContributionAmount.Amount * percentageElement.GetDecimal() / 100;
                    }
                }
            }
            catch
            {
                _logger.LogWarning("Failed to parse penalty rule for group {GroupId}", group.Id);
                return;
            }

            if (penaltyAmount > 0)
            {
                _logger.LogInformation(
                    "STUB: Apply late fee of R{Amount} to member {MemberId} in group {GroupId}",
                    penaltyAmount, member.MemberId, group.Id);

                // In production, would create a penalty charge record
                // await _penaltyRepository.CreatePenaltyAsync(...)
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply late fee to member {MemberId}", member.MemberId);
        }
    }

    private async Task NotifyChairpersonOfMissedPaymentAsync(
        StokvelsGroup group,
        GroupMember member,
        int daysPastDue,
        CancellationToken cancellationToken)
    {
        try
        {
            var chairperson = await _context.GroupMembers
                .Include(gm => gm.Member)
                .FirstOrDefaultAsync(gm => gm.GroupId == group.Id && 
                                          gm.Role == "Chairperson" && 
                                          gm.IsActive, cancellationToken);

            if (chairperson != null)
            {
                var phoneNumber = chairperson.Member.PhoneNumber;
                
                _logger.LogInformation(
                    "STUB: Notify Chairperson {PhoneNumber} of missed payment: Member {MemberId} is {Days} days past due",
                    phoneNumber, member.MemberId, daysPastDue);

                // In production, would send actual notification
                await Task.Delay(10, cancellationToken); // Simulate notification
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify Chairperson of missed payment for member {MemberId}", member.MemberId);
        }
    }
}
