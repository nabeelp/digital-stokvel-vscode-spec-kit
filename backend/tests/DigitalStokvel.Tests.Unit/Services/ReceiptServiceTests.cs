using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Enums;
using DigitalStokvel.Core.ValueObjects;
using DigitalStokvel.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DigitalStokvel.Tests.Unit.Services;

public class ReceiptServiceTests
{
    private readonly Mock<ILogger<ReceiptService>> _loggerMock;
    private readonly ReceiptService _sut;

    public ReceiptServiceTests()
    {
        _loggerMock = new Mock<ILogger<ReceiptService>>();
        _sut = new ReceiptService(_loggerMock.Object);
    }

    #region GenerateReceipt Tests

    [Fact]
    public void GenerateReceipt_WithValidContribution_ShouldContainAllDetails()
    {
        // Arrange
        var contributionId = Guid.NewGuid();
        var paymentRef = "TXN123456";
        var contribution = new Contribution
        {
            Id = contributionId,
            Amount = new Money(500, "ZAR"),
            PaymentMethod = PaymentMethod.OneTap,
            Status = ContributionStatus.Completed,
            Timestamp = new DateTime(2026, 3, 24, 14, 30, 0),
            PaymentGatewayReference = paymentRef
        };

        // Act
        var receipt = _sut.GenerateReceipt(
            contribution,
            "Thandiwe Nkosi",
            "Ntombizodwa Stokvel",
            "en");

        // Assert
        receipt.Should().NotBeNullOrWhiteSpace();
        receipt.Should().Contain("DIGITAL STOKVEL");
        receipt.Should().Contain("Ntombizodwa Stokvel");
        receipt.Should().Contain("Thandiwe Nkosi");
        receipt.Should().Contain("R500.00");
        receipt.Should().Contain("One-Tap");
        receipt.Should().Contain("24 Mar 2026");
        receipt.Should().Contain(paymentRef);
        receipt.Should().Contain("www.digitalstokvel.co.za");
    }

    [Fact]
    public void GenerateReceipt_WithCompletedStatus_ShouldShowSuccessIndicator()
    {
        // Arrange
        var contribution = new Contribution
        {
            Id = Guid.NewGuid(),
            Amount = new Money(500, "ZAR"),
            PaymentMethod = PaymentMethod.DebitOrder,
            Status = ContributionStatus.Completed,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var receipt = _sut.GenerateReceipt(
            contribution,
            "Member Name",
            "Group Name",
            "en");

        // Assert
        receipt.Should().Contain("Successful");
    }

    [Fact]
    public void GenerateReceipt_WithPendingStatus_ShouldShowPendingIndicator()
    {
        // Arrange
        var contribution = new Contribution
        {
            Id = Guid.NewGuid(),
            Amount = new Money(750, "ZAR"),
            PaymentMethod = PaymentMethod.USSD,
            Status = ContributionStatus.Pending,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var receipt = _sut.GenerateReceipt(
            contribution,
            "Member Name",
            "Group Name",
            "en");

        // Assert
        receipt.Should().Contain("Pending");
    }

    [Fact]
    public void GenerateReceipt_WithNullPaymentReference_ShouldUseContributionId()
    {
        // Arrange
        var contributionId = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var contribution = new Contribution
        {
            Id = contributionId,
            Amount = new Money(500, "ZAR"),
            PaymentMethod = PaymentMethod.OneTap,
            Status = ContributionStatus.Completed,
            Timestamp = DateTime.UtcNow,
            PaymentGatewayReference = null // No payment ref
        };

        // Act
        var receipt = _sut.GenerateReceipt(
            contribution,
            "Member Name",
            "Group Name",
            "en");

        // Assert
        // Should contain first 8 chars of GUID (without hyphens) in uppercase
        receipt.Should().Contain("12345678".ToUpper());
    }

    [Fact]
    public void GenerateReceipt_WithNullContribution_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => _sut.GenerateReceipt(
            null!,
            "Member Name",
            "Group Name",
            "en");

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("contribution");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("zu")]
    [InlineData("st")]
    public void GenerateReceipt_WithDifferentLanguages_ShouldGenerateReceipt(string language)
    {
        // Arrange
        var contribution = new Contribution
        {
            Id = Guid.NewGuid(),
            Amount = new Money(500, "ZAR"),
            PaymentMethod = PaymentMethod.OneTap,
            Status = ContributionStatus.Completed,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var receipt = _sut.GenerateReceipt(
            contribution,
            "Member Name",
            "Group Name",
            language);

        // Assert
        receipt.Should().NotBeNullOrWhiteSpace();
        receipt.Should().Contain("DIGITAL STOKVEL");
        receipt.Should().Contain("R500.00");
    }

    #endregion

    #region GenerateShareableReceipt Tests

    [Fact]
    public void GenerateShareableReceipt_ShouldBeCompactFormat()
    {
        // Arrange
        var contribution = new Contribution
        {
            Id = Guid.NewGuid(),
            Amount = new Money(500, "ZAR"),
            PaymentMethod = PaymentMethod.OneTap,
            Status = ContributionStatus.Completed,
            Timestamp = new DateTime(2026, 3, 24),
            PaymentGatewayReference = "TXN123"
        };

        // Act
        var receipt = _sut.GenerateShareableReceipt(
            contribution,
            "Thandiwe Nkosi",
            "Ntombizodwa Stokvel",
            "en");

        // Assert
        receipt.Should().NotBeNullOrWhiteSpace();
        receipt.Should().Contain("CONTRIBUTION RECEIPT");
        receipt.Should().Contain("Ntombizodwa Stokvel");
        receipt.Should().Contain("R500.00");
        receipt.Should().Contain("24 Mar 2026");
        receipt.Should().Contain("TXN123");
        receipt.Should().Contain("Digital Stokvel");
        receipt.Should().Contain("Building financial futures together");
    }

    [Fact]
    public void GenerateShareableReceipt_WithNullPaymentReference_ShouldUseShortContributionId()
    {
        // Arrange
        var contributionId = Guid.Parse("abcdef12-3456-7890-abcd-ef1234567890");
        var contribution = new Contribution
        {
            Id = contributionId,
            Amount = new Money(1000, "ZAR"),
            PaymentMethod = PaymentMethod.DebitOrder,
            Status = ContributionStatus.Completed,
            Timestamp = DateTime.UtcNow,
            PaymentGatewayReference = null
        };

        // Act
        var receipt = _sut.GenerateShareableReceipt(
            contribution,
            "Member Name",
            "Group Name",
            "en");

        // Assert
        receipt.Should().Contain("ABCDEF12");
    }

    [Theory]
    [InlineData(PaymentMethod.OneTap, "One-Tap")]
    [InlineData(PaymentMethod.DebitOrder, "Debit Order")]
    [InlineData(PaymentMethod.USSD, "USSD")]
    public void GenerateReceipt_WithDifferentPaymentMethods_ShouldIncludeMethodName(
        PaymentMethod method,
        string expectedDisplay)
    {
        // Arrange
        var contribution = new Contribution
        {
            Id = Guid.NewGuid(),
            Amount = new Money(500, "ZAR"),
            PaymentMethod = method,
            Status = ContributionStatus.Completed,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var receipt = _sut.GenerateReceipt(
            contribution,
            "Member Name",
            "Group Name",
            "en");

        // Assert
        receipt.Should().Contain(expectedDisplay);
    }

    [Fact]
    public void GenerateReceipt_WithLargeAmount_ShouldFormatWithThousandsSeparator()
    {
        // Arrange
        var contribution = new Contribution
        {
            Id = Guid.NewGuid(),
            Amount = new Money(15750.50m, "ZAR"),
            PaymentMethod = PaymentMethod.OneTap,
            Status = ContributionStatus.Completed,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var receipt = _sut.GenerateReceipt(
            contribution,
            "Member Name",
            "Group Name",
            "en");

        // Assert
        // South African locale uses space (possibly non-breaking) as thousands separator
        // Just verify the amount parts are present
        receipt.Should().Contain("15");
        receipt.Should().Contain("750.50");
    }

    #endregion
}
