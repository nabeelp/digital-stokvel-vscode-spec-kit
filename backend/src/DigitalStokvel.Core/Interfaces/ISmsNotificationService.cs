namespace DigitalStokvel.Core.Interfaces;

/// <summary>
/// Interface for SMS notification service
/// </summary>
public interface ISmsNotificationService
{
    /// <summary>
    /// Sends a group invitation SMS
    /// </summary>
    /// <param name="recipientPhoneNumber">Recipient's phone number in E.164 format (+27...)</param>
    /// <param name="groupName">Name of the group</param>
    /// <param name="inviteCode">Invitation code</param>
    /// <param name="contributionAmount">Contribution amount</param>
    /// <param name="language">Language code (en, zu, st, xh, af)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (Success, MessageId, ErrorMessage)</returns>
    Task<(bool Success, string? MessageId, string? ErrorMessage)> SendGroupInvitationSmsAsync(
        string recipientPhoneNumber,
        string groupName,
        string inviteCode,
        decimal contributionAmount,
        string language = "en",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a contribution confirmation SMS
    /// </summary>
    /// <param name="recipientPhoneNumber">Recipient's phone number</param>
    /// <param name="groupName">Name of the group</param>
    /// <param name="amount">Contribution amount</param>
    /// <param name="balance">New group balance</param>
    /// <param name="language">Language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (Success, MessageId, ErrorMessage)</returns>
    Task<(bool Success, string? MessageId, string? ErrorMessage)> SendContributionConfirmationSmsAsync(
        string recipientPhoneNumber,
        string groupName,
        decimal amount,
        decimal balance,
        string language = "en",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a payout notification SMS
    /// </summary>
    /// <param name="recipientPhoneNumber">Recipient's phone number</param>
    /// <param name="groupName">Name of the group</param>
    /// <param name="amount">Payout amount</param>
    /// <param name="language">Language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (Success, MessageId, ErrorMessage)</returns>
    Task<(bool Success, string? MessageId, string? ErrorMessage)> SendPayoutNotificationSmsAsync(
        string recipientPhoneNumber,
        string groupName,
        decimal amount,
        string language = "en",
        CancellationToken cancellationToken = default);
}
