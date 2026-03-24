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
public class SmsNotificationService
{
    private readonly ILogger<SmsNotificationService> _logger;
    private readonly string? _connectionString;
    private readonly string? _senderPhoneNumber;

    public SmsNotificationService(
        ILogger<SmsNotificationService> logger,
        string? connectionString = null,
        string? senderPhoneNumber = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        return language.ToLower() switch
        {
            "zu" => $"Sawubona! Umenyiwe ku-{groupName}. Ukufaka: R{amount:F2} ngenyanga. Ikhodi: {inviteCode}. Joyina manje!",
            "st" => $"Dumela! O memelitswe ho {groupName}. Sekoloto: R{amount:F2} ka kgwedi. Khoutu: {inviteCode}. Ikopanye!",
            "xh" => $"Molo! Umenyiwe ku-{groupName}. Igalelo: R{amount:F2} ngenyanga. Ikhowudi: {inviteCode}. Joyina ngoku!",
            "af" => $"Hallo! Jy is genooi na {groupName}. Bydrae: R{amount:F2} per maand. Kode: {inviteCode}. Sluit nou aan!",
            _ => $"Hi! You're invited to {groupName}. Contribution: R{amount:F2}/month. Code: {inviteCode}. Join now!"
        };
    }

    /// <summary>
    /// Builds localized contribution confirmation message
    /// </summary>
    private string BuildContributionConfirmationMessage(string groupName, decimal amount, decimal balance, string language)
    {
        return language.ToLower() switch
        {
            "zu" => $"Siyabonga! Ukufaka kwakho R{amount:F2} ku-{groupName} kuyaphumelela. Ibhalansi: R{balance:F2}.",
            "st" => $"Kea leboha! Sekoloto sa hao sa R{amount:F2} ho {groupName} se atlehile. Tekanyetso: R{balance:F2}.",
            "xh" => $"Enkosi! Igalelo lakho le-R{amount:F2} ku-{groupName} liphumelele. Ibhalansi: R{balance:F2}.",
            "af" => $"Dankie! Jou bydrae van R{amount:F2} aan {groupName} is suksesvol. Balans: R{balance:F2}.",
            _ => $"Thank you! Your R{amount:F2} contribution to {groupName} was successful. Balance: R{balance:F2}."
        };
    }

    /// <summary>
    /// Builds localized payout notification message
    /// </summary>
    private string BuildPayoutNotificationMessage(string groupName, decimal amount, string language)
    {
        return language.ToLower() switch
        {
            "zu" => $"Halala! Ukholwa R{amount:F2} kusuka ku-{groupName} kuyeza. Bheka i-akhawunti yakho maduze!",
            "st" => $"Kgotlelelang! Tefo ya R{amount:F2} ho tswa ho {groupName} e nne teng. Hlahloba akhaonto ya hao!",
            "xh" => $"Uyavuya! Intlawulo ye-R{amount:F2} evela ku-{groupName} iyeza. Khangela iakhawunti yakho!",
            "af" => $"Geluk! 'n Uitbetaling van R{amount:F2} van {groupName} is onderweg. Kyk jou rekening!",
            _ => $"Congratulations! A payout of R{amount:F2} from {groupName} is on the way. Check your account!"
        };
    }

    #endregion
}
