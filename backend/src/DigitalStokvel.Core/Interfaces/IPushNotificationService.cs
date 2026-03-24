using DigitalStokvel.Core.Entities;

namespace DigitalStokvel.Core.Interfaces;

/// <summary>
/// Service for sending push notifications to mobile devices
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Send a payment reminder push notification to a member
    /// </summary>
    /// <param name="member">Target member</param>
    /// <param name="group">Group requiring payment</param>
    /// <param name="daysUntilDue">Number of days until payment is due</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status, notification ID, and error message if failed</returns>
    Task<(bool Success, string? NotificationId, string? ErrorMessage)> SendPaymentReminderAsync(
        Member member,
        StokvelsGroup group,
        int daysUntilDue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a group invitation push notification
    /// </summary>
    /// <param name="phoneNumber">Target member phone number</param>
    /// <param name="groupName">Name of the group</param>
    /// <param name="inviterName">Name of the person inviting</param>
    /// <param name="joinLink">Deep link to join group</param>
    /// <param name="language">Preferred language code (EN, ZU, ST, XH, AF)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status, notification ID, and error message if failed</returns>
    Task<(bool Success, string? NotificationId, string? ErrorMessage)> SendGroupInvitationAsync(
        string phoneNumber,
        string groupName,
        string inviterName,
        string joinLink,
        string language,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a contribution confirmation push notification
    /// </summary>
    /// <param name="member">Member who made the contribution</param>
    /// <param name="contribution">Contribution details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status, notification ID, and error message if failed</returns>
    Task<(bool Success, string? NotificationId, string? ErrorMessage)> SendContributionConfirmationAsync(
        Member member,
        Contribution contribution,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a payout notification to all group members
    /// </summary>
    /// <param name="group">Group with payout</param>
    /// <param name="payoutAmount">Amount being paid out</param>
    /// <param name="recipientName">Name of payout recipient</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of successful notifications sent</returns>
    Task<int> SendPayoutNotificationToGroupAsync(
        StokvelsGroup group,
        decimal payoutAmount,
        string recipientName,
        CancellationToken cancellationToken = default);
}
