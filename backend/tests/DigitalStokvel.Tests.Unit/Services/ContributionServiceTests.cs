using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Enums;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Core.ValueObjects;
using DigitalStokvel.Infrastructure.Data;
using DigitalStokvel.Infrastructure.Notifications;
using DigitalStokvel.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace DigitalStokvel.Tests.Unit.Services;

public class ContributionServiceTests : IDisposable
{
    private readonly Mock<IContributionRepository> _contributionRepositoryMock;
    private readonly Mock<IGroupRepository> _groupRepositoryMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IPaymentGateway> _paymentGatewayMock;
    private readonly Mock<SmsNotificationService> _smsNotificationServiceMock;
    private readonly Mock<ILogger<ContributionService>> _loggerMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly ContributionService _sut;

    public ContributionServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemory(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        _contributionRepositoryMock = new Mock<IContributionRepository>();
        _groupRepositoryMock = new Mock<IGroupRepository>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _paymentGatewayMock = new Mock<IPaymentGateway>();
        _smsNotificationServiceMock = new Mock<SmsNotificationService>();
        _loggerMock = new Mock<ILogger<ContributionService>>();

        _sut = new ContributionService(
            _contributionRepositoryMock.Object,
            _groupRepositoryMock.Object,
            _memberRepositoryMock.Object,
            _paymentGatewayMock.Object,
            _dbContext,
            _smsNotificationServiceMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region ProcessContributionAsync Tests

    [Fact]
    public async Task ProcessContributionAsync_WithIdempotentRequest_ShouldReturnCachedResult()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var existingContribution = new Contribution
        {
            Id = Guid.NewGuid(),
            Status = ContributionStatus.Completed,
            Amount = new Money(500, "ZAR"),
            IdempotencyKey = idempotencyKey
        };

        _contributionRepositoryMock
            .Setup(x => x.IdempotencyKeyExistsAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _contributionRepositoryMock
            .Setup(x => x.GetByIdempotencyKeyAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingContribution);

        // Act
        var result = await _sut.ProcessContributionAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            500m,
            PaymentMethod.OneTap,
            idempotencyKey);

        // Assert
        result.Success.Should().BeTrue();
        result.Contribution.Should().Be(existingContribution);
        result.ErrorMessage.Should().BeNull();

        // Verify payment gateway was NOT called
        _paymentGatewayMock.Verify(
            x => x.DeductFromAccountAsync(
                It.IsAny<Guid>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessContributionAsync_WithNonExistentMember_ShouldReturnError()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();

        _contributionRepositoryMock
            .Setup(x => x.IdempotencyKeyExistsAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        // Act
        var result = await _sut.ProcessContributionAsync(
            memberId,
            Guid.NewGuid(),
            500m,
            PaymentMethod.OneTap,
            idempotencyKey);

        // Assert
        result.Success.Should().BeFalse();
        result.Contribution.Should().BeNull();
        result.ErrorMessage.Should().Be("Member not found");
    }

    [Fact]
    public async Task ProcessContributionAsync_WithNonExistentGroup_ShouldReturnError()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();

        _contributionRepositoryMock
            .Setup(x => x.IdempotencyKeyExistsAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { Id = memberId });

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StokvelsGroup?)null);

        // Act
        var result = await _sut.ProcessContributionAsync(
            memberId,
            groupId,
            500m,
            PaymentMethod.OneTap,
            idempotencyKey);

        // Assert
        result.Success.Should().BeFalse();
        result.Contribution.Should().BeNull();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task ProcessContributionAsync_WithInactiveGroup_ShouldReturnError()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();

        _contributionRepositoryMock
            .Setup(x => x.IdempotencyKeyExistsAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { Id = memberId });

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StokvelsGroup { Id = groupId, IsActive = false });

        // Act
        var result = await _sut.ProcessContributionAsync(
            memberId,
            groupId,
            500m,
            PaymentMethod.OneTap,
            idempotencyKey);

        // Assert
        result.Success.Should().BeFalse();
        result.Contribution.Should().BeNull();
        result.ErrorMessage.Should().Contain("inactive");
    }

    [Fact]
    public async Task ProcessContributionAsync_WithAmountMismatch_ShouldReturnError()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();

        _contributionRepositoryMock
            .Setup(x => x.IdempotencyKeyExistsAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { Id = memberId, PreferredLanguage = "en" });

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StokvelsGroup 
            { 
                Id = groupId, 
                IsActive = true,
                ContributionAmount = new Money(500, "ZAR")
            });

        // Act
        var result = await _sut.ProcessContributionAsync(
            memberId,
            groupId,
            750m, // Wrong amount
            PaymentMethod.OneTap,
            idempotencyKey);

        // Assert
        result.Success.Should().BeFalse();
        result.Contribution.Should().BeNull();
        result.ErrorMessage.Should().Contain("500");
    }

    [Fact]
    public async Task ProcessContributionAsync_WithSuccessfulPayment_ShouldCompleteContribution()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();
        var contributionAmount = 500m;

        var member = new Member 
        { 
            Id = memberId, 
            PhoneNumber = "+27821234567",
            PreferredLanguage = "en"
        };

        var group = new StokvelsGroup
        {
            Id = groupId,
            Name = "Test Group",
            IsActive = true,
            ContributionAmount = new Money(contributionAmount, "ZAR"),
            Balance = new Money(1000, "ZAR")
        };

        _contributionRepositoryMock
            .Setup(x => x.IdempotencyKeyExistsAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        _contributionRepositoryMock
            .Setup(x => x.AddContributionAsync(It.IsAny<Contribution>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _paymentGatewayMock
            .Setup(x => x.DeductFromAccountAsync(
                memberId,
                contributionAmount,
                "ZAR",
                idempotencyKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult(
                Success: true,
                TransactionReference: "TXN123",
                ErrorMessage: null,
                ErrorCode: null,
                Timestamp: DateTime.UtcNow));

        _groupRepositoryMock
            .Setup(x => x.UpdateGroupBalanceAsync(groupId, It.IsAny<Money>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _smsNotificationServiceMock
            .Setup(x => x.SendContributionConfirmationSmsAsync(
                member.PhoneNumber,
                group.Name,
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                member.PreferredLanguage,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ProcessContributionAsync(
            memberId,
            groupId,
            contributionAmount,
            PaymentMethod.OneTap,
            idempotencyKey);

        // Assert
        result.Success.Should().BeTrue();
        result.Contribution.Should().NotBeNull();
        result.Contribution!.Status.Should().Be(ContributionStatus.Completed);
        result.Contribution.PaymentGatewayReference.Should().Be("TXN123");
        result.ErrorMessage.Should().BeNull();

        _paymentGatewayMock.Verify(
            x => x.DeductFromAccountAsync(
                memberId,
                contributionAmount,
                "ZAR", 
                idempotencyKey,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _groupRepositoryMock.Verify(
            x => x.UpdateGroupBalanceAsync(groupId, It.IsAny<Money>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _smsNotificationServiceMock.Verify(
            x => x.SendContributionConfirmationSmsAsync(
                member.PhoneNumber,
                group.Name,
                contributionAmount,
                It.Is<decimal>(balance => balance == 1000m + contributionAmount),
                member.PreferredLanguage,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessContributionAsync_WithFailedOneTapPayment_ShouldMarkAsFailed()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();

        _contributionRepositoryMock
            .Setup(x => x.IdempotencyKeyExistsAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { Id = memberId, PreferredLanguage = "en" });

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StokvelsGroup
            {
                Id = groupId,
                IsActive = true,
                ContributionAmount = new Money(500, "ZAR")
            });

        _contributionRepositoryMock
            .Setup(x => x.AddContributionAsync(It.IsAny<Contribution>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _paymentGatewayMock
            .Setup(x => x.DeductFromAccountAsync(
                It.IsAny<Guid>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult(
                Success: false,
                TransactionReference: null,
                ErrorMessage: "Insufficient funds",
                ErrorCode: "INSUFFICIENT_FUNDS",
                Timestamp: DateTime.UtcNow));

        // Act
        var result = await _sut.ProcessContributionAsync(
            memberId,
            groupId,
            500m,
            PaymentMethod.OneTap,
            idempotencyKey);

        // Assert
        result.Success.Should().BeFalse();
        result.Contribution.Should().NotBeNull();
        result.Contribution!.Status.Should().Be(ContributionStatus.Failed);
        result.Contribution.ErrorMessage.Should().Contain("Insufficient funds");
        result.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessContributionAsync_WithFailedDebitOrderPayment_ShouldMarkAsRetrying()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();

        var member = new Member 
        { 
            Id = memberId, 
            PhoneNumber = "+27821234567",
            PreferredLanguage = "en"
        };

        var group = new StokvelsGroup
        {
            Id = groupId,
            Name = "Test Group",
            IsActive = true,
            ContributionAmount = new Money(500, "ZAR")
        };

        _contributionRepositoryMock
            .Setup(x => x.IdempotencyKeyExistsAsync(idempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        _contributionRepositoryMock
            .Setup(x => x.AddContributionAsync(It.IsAny<Contribution>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _paymentGatewayMock
            .Setup(x => x.DeductFromAccountAsync(
                It.IsAny<Guid>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResult(
                Success: false,
                TransactionReference: null,
                ErrorMessage: "Insufficient funds",
                ErrorCode: "INSUFFICIENT_FUNDS",
                Timestamp: DateTime.UtcNow));

        _smsNotificationServiceMock
            .Setup(x => x.SendContributionConfirmationSmsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ProcessContributionAsync(
            memberId,
            groupId,
            500m,
            PaymentMethod.DebitOrder, // Debit order gets retry status
            idempotencyKey);

        // Assert
        result.Success.Should().BeFalse();
        result.Contribution.Should().NotBeNull();
        result.Contribution!.Status.Should().Be(ContributionStatus.Retrying);
        result.Contribution.NextRetryAt.Should().NotBeNull();
        result.Contribution.NextRetryAt.Should().BeCloseTo(
            DateTime.UtcNow.AddHours(48), 
            TimeSpan.FromMinutes(1));
        
        // Verify retry notification was sent
        _smsNotificationServiceMock.Verify(
            x => x.SendContributionConfirmationSmsAsync(
                member.PhoneNumber,
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<decimal>(),
                member.PreferredLanguage,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
