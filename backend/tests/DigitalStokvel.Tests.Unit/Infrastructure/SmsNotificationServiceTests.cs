using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Infrastructure.Notifications;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigitalStokvel.Tests.Unit.Infrastructure;

public class SmsNotificationServiceTests
{
    private readonly Mock<ILogger<SmsNotificationService>> _mockLogger;
    private readonly Mock<ILocalizationService> _mockLocalizationService;
    private readonly SmsNotificationService _sut;

    public SmsNotificationServiceTests()
    {
        _mockLogger = new Mock<ILogger<SmsNotificationService>>();
        _mockLocalizationService = new Mock<ILocalizationService>();
        
        // Setup localization responses to return templates that will be formatted by the calling code
        _mockLocalizationService
            .Setup(x => x.GetString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, string lang, object[] args) =>
            {
                // Return templates without formatting placeholders replaced
                var templates = new Dictionary<(string, string), string>
                {
                    // Invitation messages (format: groupName, amount, inviteCode)
                    {("notification.sms.invitation", "en"), "Hi! You're invited to {0}. Contribution: R{1}/month. Code: {2}. Join now!"},
                    {("notification.sms.invitation", "zu"), "Sawubona! Umenyiwe ku-{0}. Ukufaka: R{1} ngenyanga. Ikhodi: {2}. Joyina manje!"},
                    {("notification.sms.invitation", "st"), "Dumela! O memelitswe ho {0}. Sekoloto: R{1} ka kgwedi. Khoutu: {2}. Ikopanye!"},
                    {("notification.sms.invitation", "xh"), "Molo! Umenyiwe ku-{0}. Igalelo: R{1} ngenyanga. Ikhowudi: {2}. Joyina ngoku!"},
                    {("notification.sms.invitation", "af"), "Hallo! Jy is genooi na {0}. Bydrae: R{1} per maand. Kode: {2}. Sluit nou aan!"},
                    
                    // Contribution confirmation messages (format: amount, groupName, balance)
                    {("notification.sms.contribution_confirmed", "en"), "Thank you! Your R{0} contribution to {1} was successful. Balance: R{2}."},
                    {("notification.sms.contribution_confirmed", "zu"), "Siyabonga! Ukufaka kwakho R{0} ku-{1} kuyaphumelela. Ibhalansi: R{2}."},
                    {("notification.sms.contribution_confirmed", "st"), "Kea leboha! Sekoloto sa hao sa R{0} ho {1} se atlehile. Tekanyetso: R{2}."},
                    {("notification.sms.contribution_confirmed", "xh"), "Enkosi! Igalelo lakho le-R{0} ku-{1} liphumelele. Ibhalansi: R{2}."},
                    {("notification.sms.contribution_confirmed", "af"), "Dankie! Jou bydrae van R{0} aan {1} is suksesvol. Balans: R{2}."},
                    
                    // Payout notification messages (format: amount, groupName)
                    {("notification.sms.payout_notification", "en"), "Congratulations! A payout of R{0} from {1} is on the way. Check your account!"},
                    {("notification.sms.payout_notification", "zu"), "Halala! Ukholwa R{0} kusuka ku-{1} kuyeza. Bheka i-akhawunti yakho maduze!"},
                    {("notification.sms.payout_notification", "st"), "Kgotlelelang! Tefo ya R{0} ho tswa ho {1} e nne teng. Hlahloba akhaonto ya hao!"},
                    {("notification.sms.payout_notification", "xh"), "Uyavuya! Intlawulo ye-R{0} evela ku-{1} iyeza. Khangela iakhawunti yakho!"},
                    {("notification.sms.payout_notification", "af"), "Geluk! 'n Uitbetaling van R{0} van {1} is onderweg. Kyk jou rekening!"},
                    
                    // Payment reminder messages (format: amount, groupName, dueDate)
                    {("notification.sms.payment_reminder", "en"), "Reminder: R{0} contribution due for {1}. Pay by {2} to avoid late fees."},
                    {("notification.sms.payment_reminder", "zu"), "Isikhumbuzo: R{0} kumelwe ku-{1}. Khokha phambi komhla {2} ukugwema izinkokhelo zokulibala."},
                    {("notification.sms.payment_reminder", "st"), "Hopotso: R{0} e hlokahala bakeng sa {1}. Lefa ka {2} ho qoba ditefiso tsa ho lla."},
                    {("notification.sms.payment_reminder", "xh"), "Isikhumbuzo: R{0} igalelo elifunekayo ku-{1}. Hlawula ngo {2} ukuphepha iintlawulo zokubambezeleka."},
                    {("notification.sms.payment_reminder", "af"), "Herinnering: R{0} bydrae verskuldig vir {1}. Betaal voor {2} om laat fooi te vermy."}
                };
                
                var templateKey = (key, lang);
                if (templates.TryGetValue(templateKey, out var template))
                {
                    // Return template without formatting - the calling code will use string.Format
                    return template;
                }
                
                return $"[{key}]";
            });
        
        _sut = new SmsNotificationService(
            _mockLogger.Object,
            _mockLocalizationService.Object,
            connectionString: null,  // Stub service doesn't need real connection
            senderPhoneNumber: "+27123456789");
    }

    #region SendGroupInvitationSmsAsync Tests

    [Fact]
    public async Task SendGroupInvitationSmsAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var phone = "+27821234567";
        var groupName = "Ubuntu Stokvel";
        var inviteCode = "ABC123";
        var amount = 100.00m;

        // Act
        var result = await _sut.SendGroupInvitationSmsAsync(
            phone, groupName, inviteCode, amount, "en");

        // Assert
        result.Success.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Theory]
    [InlineData("en", "Hi! You're invited to Ubuntu Stokvel. Contribution: R100.00/month. Code: ABC123. Join now!")]
    [InlineData("zu", "Sawubona! Umenyiwe ku-Ubuntu Stokvel. Ukufaka: R100.00 ngenyanga. Ikhodi: ABC123. Joyina manje!")]
    [InlineData("st", "Dumela! O memelitswe ho Ubuntu Stokvel. Sekoloto: R100.00 ka kgwedi. Khoutu: ABC123. Ikopanye!")]
    [InlineData("xh", "Molo! Umenyiwe ku-Ubuntu Stokvel. Igalelo: R100.00 ngenyanga. Ikhowudi: ABC123. Joyina ngoku!")]
    [InlineData("af", "Hallo! Jy is genooi na Ubuntu Stokvel. Bydrae: R100.00 per maand. Kode: ABC123. Sluit nou aan!")]
    public async Task SendGroupInvitationSmsAsync_WithDifferentLanguages_ShouldFormatCorrectly(
        string language, string expectedMessagePart)
    {
        // Arrange
        var phone = "+27821234567";
        var groupName = "Ubuntu Stokvel";
        var inviteCode = "ABC123";
        var amount = 100.00m;

        // Act
        var result = await _sut.SendGroupInvitationSmsAsync(
            phone, groupName, inviteCode, amount, language);

        // Assert
        result.Success.Should().BeTrue();
        
        // Verify the message was logged (since it's a stub implementation)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessagePart)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("123456789")]           // Missing +27
    [InlineData("+2782123456")]         // Too short
    [InlineData("+278212345678")]       // Too long
    [InlineData("+27821234ABC")]        // Contains non-digits
    [InlineData("+1234567890")]         // Wrong country code
    [InlineData("")]                    // Empty
    public async Task SendGroupInvitationSmsAsync_WithInvalidPhoneNumber_ShouldReturnFailure(string invalidPhone)
    {
        // Arrange
        var groupName = "Ubuntu Stokvel";
        var inviteCode = "ABC123";
        var amount = 100.00m;

        // Act
        var result = await _sut.SendGroupInvitationSmsAsync(
            invalidPhone, groupName, inviteCode, amount, "en");

        // Assert
        result.Success.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().Contain("Invalid phone number format");
    }

    [Fact]
    public async Task SendGroupInvitationSmsAsync_WithNullPhoneNumber_ShouldReturnFailure()
    {
        // Arrange
        string? nullPhone = null;
        var groupName = "Ubuntu Stokvel";
        var inviteCode = "ABC123";
        var amount = 100.00m;

        // Act
        var result = await _sut.SendGroupInvitationSmsAsync(
            nullPhone!, groupName, inviteCode, amount, "en");

        // Assert
        result.Success.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().Contain("Invalid phone number format");
    }

    #endregion

    #region SendContributionConfirmationSmsAsync Tests

    [Fact]
    public async Task SendContributionConfirmationSmsAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var phone = "+27821234567";
        var groupName = "Ubuntu Stokvel";
        var amount = 100.00m;
        var balance = 5000.00m;

        // Act
        var result = await _sut.SendContributionConfirmationSmsAsync(
            phone, groupName, amount, balance, "en");

        // Assert
        result.Success.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Theory]
    [InlineData("en", "Thank you! Your R100.00 contribution to Ubuntu Stokvel was successful. Balance: R5000.00.")]
    [InlineData("zu", "Siyabonga! Ukufaka kwakho R100.00 ku-Ubuntu Stokvel kuyaphumelela. Ibhalansi: R5000.00.")]
    [InlineData("st", "Kea leboha! Sekoloto sa hao sa R100.00 ho Ubuntu Stokvel se atlehile. Tekanyetso: R5000.00.")]
    [InlineData("xh", "Enkosi! Igalelo lakho le-R100.00 ku-Ubuntu Stokvel liphumelele. Ibhalansi: R5000.00.")]
    [InlineData("af", "Dankie! Jou bydrae van R100.00 aan Ubuntu Stokvel is suksesvol. Balans: R5000.00.")]
    public async Task SendContributionConfirmationSmsAsync_WithDifferentLanguages_ShouldFormatCorrectly(
        string language, string expectedMessagePart)
    {
        // Arrange
        var phone = "+27821234567";
        var groupName = "Ubuntu Stokvel";
        var amount = 100.00m;
        var balance = 5000.00m;

        // Act
        var result = await _sut.SendContributionConfirmationSmsAsync(
            phone, groupName, amount, balance, language);

        // Assert
        result.Success.Should().BeTrue();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessagePart)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendContributionConfirmationSmsAsync_WithInvalidPhoneNumber_ShouldReturnFailure()
    {
        // Arrange
        var invalidPhone = "123456789";
        var groupName = "Ubuntu Stokvel";
        var amount = 100.00m;
        var balance = 5000.00m;

        // Act
        var result = await _sut.SendContributionConfirmationSmsAsync(
            invalidPhone, groupName, amount, balance, "en");

        // Assert
        result.Success.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().Contain("Invalid phone number format");
    }

    #endregion

    #region SendPayoutNotificationSmsAsync Tests

    [Fact]
    public async Task SendPayoutNotificationSmsAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var phone = "+27821234567";
        var groupName = "Ubuntu Stokvel";
        var amount = 10000.00m;

        // Act
        var result = await _sut.SendPayoutNotificationSmsAsync(
            phone, groupName, amount, "en");

        // Assert
        result.Success.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Theory]
    [InlineData("en", "Congratulations! A payout of R10000.00 from Ubuntu Stokvel is on the way. Check your account!")]
    [InlineData("zu", "Halala! Ukholwa R10000.00 kusuka ku-Ubuntu Stokvel kuyeza. Bheka i-akhawunti yakho maduze!")]
    [InlineData("st", "Kgotlelelang! Tefo ya R10000.00 ho tswa ho Ubuntu Stokvel e nne teng. Hlahloba akhaonto ya hao!")]
    [InlineData("xh", "Uyavuya! Intlawulo ye-R10000.00 evela ku-Ubuntu Stokvel iyeza. Khangela iakhawunti yakho!")]
    [InlineData("af", "Geluk! 'n Uitbetaling van R10000.00 van Ubuntu Stokvel is onderweg. Kyk jou rekening!")]
    public async Task SendPayoutNotificationSmsAsync_WithDifferentLanguages_ShouldFormatCorrectly(
        string language, string expectedMessagePart)
    {
        // Arrange
        var phone = "+27821234567";
        var groupName = "Ubuntu Stokvel";
        var amount = 10000.00m;

        // Act
        var result = await _sut.SendPayoutNotificationSmsAsync(
            phone, groupName, amount, language);

        // Assert
        result.Success.Should().BeTrue();
        
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessagePart)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendPayoutNotificationSmsAsync_WithInvalidPhoneNumber_ShouldReturnFailure()
    {
        // Arrange
        var invalidPhone = "+1234567890";  // Wrong country code
        var groupName = "Ubuntu Stokvel";
        var amount = 10000.00m;

        // Act
        var result = await _sut.SendPayoutNotificationSmsAsync(
            invalidPhone, groupName, amount, "en");

        // Assert
        result.Success.Should().BeFalse();
        result.MessageId.Should().BeNull();
        result.ErrorMessage.Should().Contain("Invalid phone number format");
    }

    #endregion

    #region Message ID Generation Tests

    [Fact]
    public async Task SendGroupInvitationSmsAsync_ShouldGenerateUniqueMessageIds()
    {
        // Arrange
        var phone = "+27821234567";
        var groupName = "Ubuntu Stokvel";
        var inviteCode = "ABC123";
        var amount = 100.00m;

        // Act
        var result1 = await _sut.SendGroupInvitationSmsAsync(phone, groupName, inviteCode, amount);
        var result2 = await _sut.SendGroupInvitationSmsAsync(phone, groupName, inviteCode, amount);

        // Assert
        result1.MessageId.Should().NotBe(result2.MessageId);
    }

    #endregion

    #region Phone Number Validation Edge Cases

    [Theory]
    [InlineData("+27821234567")]  // Valid
    [InlineData("+27123456789")]  // Valid - different prefix
    [InlineData("+27999999999")]  // Valid - max digits
    public async Task SendGroupInvitationSmsAsync_WithValidPhoneNumberFormats_ShouldSucceed(string validPhone)
    {
        // Arrange
        var groupName = "Ubuntu Stokvel";
        var inviteCode = "ABC123";
        var amount = 100.00m;

        // Act
        var result = await _sut.SendGroupInvitationSmsAsync(
            validPhone, groupName, inviteCode, amount, "en");

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task SendGroupInvitationSmsAsync_WithCancellationToken_ShouldCompleteSuccessfully()
    {
        // Arrange
        var phone = "+27821234567";
        var groupName = "Ubuntu Stokvel";
        var inviteCode = "ABC123";
        var amount = 100.00m;
        var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.SendGroupInvitationSmsAsync(
            phone, groupName, inviteCode, amount, "en", cts.Token);

        // Assert
        // Stub implementation handles cancellation gracefully
        result.Success.Should().BeTrue();
        result.MessageId.Should().NotBeNullOrEmpty();
    }

    #endregion
}
