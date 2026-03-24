using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DigitalStokvel.Infrastructure.Jobs;

/// <summary>
/// Background job that sends payment reminders to members
/// Runs daily and sends reminders at 3 days and 1 day before payment is due
/// </summary>
public class PaymentReminderJob
{
    private readonly ILogger<PaymentReminderJob> _logger;
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IPushNotificationService _pushNotificationService;
    private readonly ISmsNotificationService _smsNotificationService;

    public PaymentReminderJob(
        ILogger<PaymentReminderJob> logger,
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        IPushNotificationService pushNotificationService,
        ISmsNotificationService smsNotificationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
        _pushNotificationService = pushNotificationService ?? throw new ArgumentNullException(nameof(pushNotificationService));
        _smsNotificationService = smsNotificationService ?? throw new ArgumentNullException(nameof(smsNotificationService));
    }

    /// <summary>
    /// Execute payment reminder job
    /// Sends reminders to members who have payments due in 3 days or 1 day
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Payment reminder job started at {Timestamp}", DateTime.UtcNow);

        try
        {
            var today = DateTime.UtcNow.Date;
            var threeDaysFromNow = today.AddDays(3);
            var oneDayFromNow = today.AddDays(1);

            // Get all active groups - in production, filter by next payment due date
            var activeGroups = await GetActiveGroupsAsync(cancellationToken);

            _logger.LogInformation("Processing payment reminders for {GroupCount} active groups", activeGroups.Count);

            int remindersSent = 0;
            int remindersFailed = 0;

            foreach (var group in activeGroups)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    // Calculate next payment due date based on contribution frequency
                    var nextPaymentDue = CalculateNextPaymentDueDate(group, today);
                    var daysUntilDue = (nextPaymentDue - today).Days;

                    // Send reminders only at 3 days and 1 day milestones
                    if (daysUntilDue != 3 && daysUntilDue != 1)
                        continue;

                    _logger.LogInformation(
                        "Sending {Days}-day reminders for group {GroupName} ({GroupId})",
                        daysUntilDue,
                        group.Name,
                        group.Id);

                    // Get all active members in the group
                    var groupMembers = await _groupRepository.GetGroupWithMembersAsync(group.Id, cancellationToken);
                    if (groupMembers?.Members == null)
                        continue;

                    foreach (var groupMember in groupMembers.Members.Where(gm => gm.IsActive))
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        var member = groupMember.Member;
                        if (member == null)
                            continue;

                        // Send push notification
                        var pushResult = await _pushNotificationService.SendPaymentReminderAsync(
                            member,
                            group,
                            daysUntilDue,
                            cancellationToken);

                        if (pushResult.Success)
                        {
                            remindersSent++;
                            _logger.LogDebug(
                                "Push reminder sent to member {MemberId} for group {GroupName}",
                                member.Id,
                                group.Name);
                        }
                        else
                        {
                            remindersFailed++;
                            _logger.LogWarning(
                                "Failed to send push reminder to member {MemberId}: {Error}",
                                member.Id,
                                pushResult.ErrorMessage);

                            // Fallback to SMS if push notification fails
                            // TODO: Implement SMS reminder method in ISmsNotificationService
                            _logger.LogInformation(
                                "Consider implementing SMS fallback for member {MemberId}",
                                member.Id);
                        }

                        // Rate limiting: small delay between notifications
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to process payment reminders for group {GroupId}",
                        group.Id);
                    remindersFailed++;
                }
            }

            _logger.LogInformation(
                "Payment reminder job completed. Sent: {Sent}, Failed: {Failed}",
                remindersSent,
                remindersFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment reminder job failed");
            throw;
        }
    }

    /// <summary>
    /// Get all active groups that require payment reminders
    /// </summary>
    private async Task<List<StokvelsGroup>> GetActiveGroupsAsync(CancellationToken cancellationToken)
    {
        // In production: use repository method with filtering by next payment date
        // For now, stub implementation returns empty list (would need proper repository method)
        _logger.LogWarning("[STUB] GetActiveGroupsAsync - returning empty list. Implement proper repository query.");
        return await Task.FromResult(new List<StokvelsGroup>());
    }

    /// <summary>
    /// Calculate the next payment due date based on group's contribution frequency
    /// </summary>
    /// <param name="group">Group with contribution schedule</param>
    /// <param name="fromDate">Calculate from this date</param>
    /// <returns>Next payment due date</returns>
    private DateTime CalculateNextPaymentDueDate(StokvelsGroup group, DateTime fromDate)
    {
        // Stub implementation - in production:
        // 1. Load last successful contribution date for the group
        // 2. Add interval based on ContributionFrequency (Weekly, Biweekly, Monthly)
        // 3. Handle edge cases (weekends, holidays, grace periods)

        return group.ContributionFrequency.ToLowerInvariant() switch
        {
            "weekly" => fromDate.AddDays(7),
            "biweekly" => fromDate.AddDays(14),
            "monthly" => fromDate.AddMonths(1),
            _ => fromDate.AddMonths(1) // Default to monthly
        };
    }
}
