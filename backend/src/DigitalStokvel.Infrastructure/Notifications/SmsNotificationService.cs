using DigitalStokvel.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DigitalStokvel.Infrastructure.Notifications;

/// <summary>
/// Service for sending SMS notifications using Azure Communication Services
/// </summary>
/// <remarks>
/// This is a stub implementation. In production, integrate with Azure Communication Services SDK:
/// 1. Install package: Azure.Communication.Sms
/// 2. Configure connection string in appsettings.json
/// 3. Use SmsClient to send SMS messages
/// </remarks>
public class SmsNotificationService : ISmsNotificationService
{
    private readonly ILogger<SmsNotificationService> _logger;
    private readonly ILocalizationService _localizationService;
    private readonly string? _connectionString;
    private readonly string? _senderPhoneNumber;

    public SmsNotificationService(
        ILogger<SmsNotificationService> logger,
        ILocalizationService localizationService,
        string? connectionString = null,
        string? senderPhoneNumber = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _connectionString = connectionString;
        _senderPhoneNumber = senderPhoneNumber ?? "+27600000000"; // Default sender (stub)
    }

    /// <summary>
    /// Sends a group invitation SMS
    /// </summary>
    /// <param name="recipientPhoneNumber">Recipient's phone number in E.164 format (+27...)</param>
    /// <param name="groupName">Name of the group</param>
    /// <param name="inviteCode">Invitation code</param>
    /// <param name="contributionAmount">Contribution amount</param>
    /// <param name="language">Language code (en, zu, st, xh, af)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if SMS sent successfully</returns>
    public async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendGroupInvitationSmsAsync(
        string recipientPhoneNumber,
        string groupName,
        string inviteCode,
        decimal contributionAmount,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate phone number format (South African E.164)
            if (!IsValidSouthAfricanPhoneNumber(recipientPhoneNumber))
            {
                return (false, null, "Invalid phone number format. Must start with +27");
            }

            // Build localized message
            var message = BuildInvitationMessage(groupName, inviteCode, contributionAmount, language);

            // STUB: Log the SMS details instead of actually sending
            // In production, replace with actual Azure Communication Services call:
            // var smsClient = new SmsClient(_connectionString);
            // var sendResult = await smsClient.SendAsync(
            //     from: _senderPhoneNumber,
            //     to: recipientPhoneNumber,
            //     message: message,
            //     cancellationToken: cancellationToken);

            var messageId = Guid.NewGuid().ToString("N");

            _logger.LogInformation(
                "STUB: SMS Invitation Sent | To: {RecipientPhone} | Group: {GroupName} | Code: {InviteCode} | MessageId: {MessageId}",
                recipientPhoneNumber, groupName, inviteCode, messageId);

            _logger.LogDebug("SMS Content: {Message}", message);

            // Simulate async operation
            await Task.Delay(100, cancellationToken);

            return (true, messageId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS invitation to {RecipientPhone}", recipientPhoneNumber);
            return (false, null, "Failed to send SMS notification");
        }
    }

    /// <summary>
    /// Sends a contribution confirmation SMS
    /// </summary>
    /// <param name="recipientPhoneNumber">Recipient's phone number</param>
    /// <param name="groupName">Name of the group</param>
    /// <param name="amount">Contribution amount</param>
    /// <param name="balance">New group balance</param>
    /// <param name="language">Language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if SMS sent successfully</returns>
    public async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendContributionConfirmationSmsAsync(
        string recipientPhoneNumber,
        string groupName,
        decimal amount,
        decimal balance,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsValidSouthAfricanPhoneNumber(recipientPhoneNumber))
            {
                return (false, null, "Invalid phone number format");
            }

            var message = BuildContributionConfirmationMessage(groupName, amount, balance, language);

            var messageId = Guid.NewGuid().ToString("N");

            _logger.LogInformation(
                "STUB: SMS Contribution Confirmation | To: {RecipientPhone} | Amount: R{Amount} | MessageId: {MessageId}",
                recipientPhoneNumber, amount, messageId);

            _logger.LogDebug("SMS Content: {Message}", message);

            await Task.Delay(100, cancellationToken);

            return (true, messageId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contribution confirmation SMS to {RecipientPhone}", recipientPhoneNumber);
            return (false, null, "Failed to send SMS notification");
        }
    }

    /// <summary>
    /// Sends a payout notification SMS
    /// </summary>
    /// <param name="recipientPhoneNumber">Recipient's phone number</param>
    /// <param name="groupName">Name of the group</param>
    /// <param name="amount">Payout amount</param>
    /// <param name="language">Language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if SMS sent successfully</returns>
    public async Task<(bool Success, string? MessageId, string? ErrorMessage)> SendPayoutNotificationSmsAsync(
        string recipientPhoneNumber,
        string groupName,
        decimal amount,
        string language = "en",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsValidSouthAfricanPhoneNumber(recipientPhoneNumber))
            {
                return (false, null, "Invalid phone number format");
            }

            var message = BuildPayoutNotificationMessage(groupName, amount, language);

            var messageId = Guid.NewGuid().ToString("N");

            _logger.LogInformation(
                "STUB: SMS Payout Notification | To: {RecipientPhone} | Amount: R{Amount} | MessageId: {MessageId}",
                recipientPhoneNumber, amount, messageId);

            _logger.LogDebug("SMS Content: {Message}", message);

            await Task.Delay(100, cancellationToken);

            return (true, messageId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payout notification SMS to {RecipientPhone}", recipientPhoneNumber);
            return (false, null, "Failed to send SMS notification");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Validates South African phone number format (E.164)
    /// </summary>
    private bool IsValidSouthAfricanPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Must start with +27 and have 11 total digits (including +27)
        return phoneNumber.StartsWith("+27") && phoneNumber.Length == 12 && 
               phoneNumber[3..].All(char.IsDigit);
    }

    /// <summary>
    /// Builds localized invitation message
    /// </summary>
    private string BuildInvitationMessage(string groupName, string inviteCode, decimal amount, string language)
    {
        var template = _localizationService.GetString("notification.sms.invitation", language);
        return string.Format(template, groupName, amount.ToString("F2"), inviteCode);
    }

    /// <summary>
    /// Builds localized contribution confirmation message
    /// </summary>
    private string BuildContributionConfirmationMessage(string groupName, decimal amount, decimal balance, string language)
    {
        var template = _localizationService.GetString("notification.sms.contribution_confirmed", language);
        return string.Format(template, amount.ToString("F2"), groupName, balance.ToString("F2"));
    }

    /// <summary>
    /// Builds localized payout notification message
    /// </summary>
    private string BuildPayoutNotificationMessage(string groupName, decimal amount, string language)
    {
        var template = _localizationService.GetString("notification.sms.payout_notification", language);
        return string.Format(template, amount.ToString("F2"), "member", groupName);
    }

    #endregion
}
