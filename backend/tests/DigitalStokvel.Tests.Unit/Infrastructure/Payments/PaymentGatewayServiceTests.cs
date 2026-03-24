using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Infrastructure.Payments;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DigitalStokvel.Tests.Unit.Infrastructure.Payments;

public class PaymentGatewayServiceTests
{
    private readonly Mock<ILogger<PaymentGatewayService>> _loggerMock;
    private readonly PaymentGatewayService _service;
    private const string TestApiEndpoint = "https://api.test-bank.za/payments";
    private const string TestApiKey = "test-key-12345";

    public PaymentGatewayServiceTests()
    {
        _loggerMock = new Mock<ILogger<PaymentGatewayService>>();
        
        _service = new PaymentGatewayService(
            _loggerMock.Object,
            TestApiEndpoint,
            TestApiKey);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new PaymentGatewayService(
            null!,
            TestApiEndpoint,
            TestApiKey);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldNotThrow_WhenApiEndpointIsNull()
    {
        // Act
        var act = () => new PaymentGatewayService(
            _loggerMock.Object,
            null,
            TestApiKey);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ShouldNotThrow_WhenApiKeyIsNull()
    {
        // Act
        var act = () => new PaymentGatewayService(
            _loggerMock.Object,
            TestApiEndpoint,
            null);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region DeductFromAccountAsync Tests

    [Fact]
    public async Task DeductFromAccountAsync_ShouldReturnSuccess_WhenPaymentSucceeds()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var amount = 100m;
        var idempotencyKey = Guid.NewGuid().ToString();

        // Act
        var result = await _service.DeductFromAccountAsync(
            memberId,
            amount,
            "ZAR",
            idempotencyKey,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        
        // Either success or failure (95% success, 5% failure)
        if (result.Success)
        {
            result.TransactionReference.Should().NotBeNullOrEmpty();
            result.TransactionReference.Should().StartWith("PAY-");
            result.ErrorMessage.Should().BeNullOrEmpty();
            result.ErrorCode.Should().BeNullOrEmpty();
        }
        else
        {
            result.TransactionReference.Should().NotBeNullOrEmpty();
            result.ErrorMessage.Should().Be("Insufficient funds");
            result.ErrorCode.Should().Be("INSUFFICIENT_FUNDS");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(-100.50)]
    public async Task DeductFromAccountAsync_ShouldReturnFailure_WhenAmountIsInvalid(decimal amount)
    {
        // Arrange
        var memberId = Guid.NewGuid();

        // Act
        var result = await _service.DeductFromAccountAsync(
            memberId,
            amount,
            "ZAR",
            null,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid amount");
        result.ErrorCode.Should().Be("INVALID_AMOUNT");
        result.TransactionReference.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task DeductFromAccountAsync_ShouldGenerateTransactionReference_WhenSuccessful()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var amount = 50m;
        PaymentResult? successResult = null;

        // Act - Run multiple times to catch at least one success (95% success rate)
        for (int i = 0; i < 20; i++)
        {
            var result = await _service.DeductFromAccountAsync(
                memberId,
                amount,
                "ZAR",
                null,
                CancellationToken.None);

            if (result.Success)
            {
                successResult = result;
                break;
            }
        }

        // Assert
        successResult.Should().NotBeNull("at least one payment should succeed in 20 attempts");
        successResult!.TransactionReference.Should().NotBeNullOrEmpty();
        successResult.TransactionReference.Should().StartWith("PAY-");
        successResult.TransactionReference.Should().MatchRegex(@"^PAY-[a-f0-9]{32}$");
    }

    [Fact]
    public async Task DeductFromAccountAsync_ShouldLogPaymentAttempt()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var amount = 75m;

        // Act
        await _service.DeductFromAccountAsync(
            memberId,
            amount,
            "ZAR",
            null,
            CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("STUB: Payment")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeductFromAccountAsync_ShouldHandleCancellation()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var amount = 100m;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _service.DeductFromAccountAsync(
            memberId,
            amount,
            "ZAR",
            null,
            cts.Token);

        // Assert
        // Cancellation is caught in try-catch and returns gateway error
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("GATEWAY_ERROR");
        result.ErrorMessage.Should().Be("Payment gateway communication error");
    }

    [Fact]
    public async Task DeductFromAccountAsync_ShouldUseDefaultCurrency_WhenNotSpecified()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var amount = 100m;

        // Act
        var result = await _service.DeductFromAccountAsync(
            memberId,
            amount);

        // Assert
        result.Should().NotBeNull();
        
        // Verify that ZAR was used (check logs)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ZAR")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task DeductFromAccountAsync_ShouldIncludeIdempotencyKey_WhenProvided()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var amount = 100m;
        var idempotencyKey = "unique-key-123";

        // Act
        await _service.DeductFromAccountAsync(
            memberId,
            amount,
            "ZAR",
            idempotencyKey);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(idempotencyKey)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region SetupDebitOrderAsync Tests

    [Fact]
    public async Task SetupDebitOrderAsync_ShouldReturnSuccess_WhenSetupSucceeds()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var amount = 200m;
        var startDate = DateTime.UtcNow.AddDays(7);

        // Act
        var result = await _service.SetupDebitOrderAsync(
            memberId,
            amount,
            "Monthly",
            startDate,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.DebitOrderReference.Should().NotBeNullOrEmpty();
        result.DebitOrderReference.Should().StartWith("DO-");
        result.NextDebitDate.Should().NotBeNull();
        result.NextDebitDate.Should().BeOnOrAfter(DateTime.UtcNow.Date);
        result.ErrorMessage.Should().BeNullOrEmpty();
    }

    [Theory]
    [InlineData("Monthly")]
    [InlineData("Biweekly")]
    [InlineData("Weekly")]
    public async Task SetupDebitOrderAsync_ShouldHandleDifferentFrequencies(string frequency)
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var amount = 150m;
        var startDate = DateTime.UtcNow.AddDays(5);

        // Act
        var result = await _service.SetupDebitOrderAsync(
            memberId,
            amount,
            frequency,
            startDate,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.DebitOrderReference.Should().NotBeNullOrEmpty();
        result.NextDebitDate.Should().NotBeNull();
    }

    [Fact]
    public async Task SetupDebitOrderAsync_ShouldCalculateNextDebitDate_WhenStartDateIsInPast()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var amount = 100m;
        var startDate = DateTime.UtcNow.AddMonths(-2); // Past date

        // Act
        var result = await _service.SetupDebitOrderAsync(
            memberId,
            amount,
            "Monthly",
            startDate,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.NextDebitDate.Should().NotBeNull();
        result.NextDebitDate.Should().BeOnOrAfter(DateTime.UtcNow.Date);
    }

    [Fact]
    public async Task SetupDebitOrderAsync_ShouldLogDebitOrderSetup()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var amount = 300m;
        var startDate = DateTime.UtcNow.AddDays(10);

        // Act
        await _service.SetupDebitOrderAsync(
            memberId,
            amount,
            "Monthly",
            startDate);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("STUB: Debit Order Setup")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SetupDebitOrderAsync_ShouldHandleCancellation()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var amount = 100m;
        var startDate = DateTime.UtcNow.AddDays(5);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _service.SetupDebitOrderAsync(
            memberId,
            amount,
            "Monthly",
            startDate,
            cts.Token);

        // Assert
        // Cancellation is caught in try-catch and returns failure
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Failed to setup debit order");
    }

    #endregion

    #region CancelDebitOrderAsync Tests

    [Fact]
    public async Task CancelDebitOrderAsync_ShouldReturnTrue_WhenCancellationSucceeds()
    {
        // Arrange
        var debitOrderRef = "DO-12345678";

        // Act
        var result = await _service.CancelDebitOrderAsync(debitOrderRef);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CancelDebitOrderAsync_ShouldLogCancellation()
    {
        // Arrange
        var debitOrderRef = "DO-87654321";

        // Act
        await _service.CancelDebitOrderAsync(debitOrderRef);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("STUB: Debit Order Cancelled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelDebitOrderAsync_ShouldHandleCancellation()
    {
        // Arrange
        var debitOrderRef = "DO-11111111";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _service.CancelDebitOrderAsync(
            debitOrderRef,
            cts.Token);

        // Assert
        // Cancellation is caught in try-catch and returns false
        result.Should().BeFalse();
    }

    #endregion
}
