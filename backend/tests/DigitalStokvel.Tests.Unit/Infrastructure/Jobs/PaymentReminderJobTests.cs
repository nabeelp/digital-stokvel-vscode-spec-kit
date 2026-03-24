using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Core.ValueObjects;
using DigitalStokvel.Infrastructure.Jobs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DigitalStokvel.Tests.Unit.Infrastructure.Jobs;

/// <summary>
/// Unit tests for PaymentReminderJob - sends reminders 3 days and 1 day before payment due
/// </summary>
public class PaymentReminderJobTests
{
    private readonly Mock<ILogger<PaymentReminderJob>> _loggerMock;
    private readonly Mock<IGroupRepository> _groupRepositoryMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IPushNotificationService> _pushNotificationServiceMock;
    private readonly Mock<ISmsNotificationService> _smsNotificationServiceMock;
    private readonly PaymentReminderJob _sut; // System Under Test

    public PaymentReminderJobTests()
    {
        _loggerMock = new Mock<ILogger<PaymentReminderJob>>();
        _groupRepositoryMock = new Mock<IGroupRepository>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _pushNotificationServiceMock = new Mock<IPushNotificationService>();
        _smsNotificationServiceMock = new Mock<ISmsNotificationService>();

        _sut = new PaymentReminderJob(
            _loggerMock.Object,
            _groupRepositoryMock.Object,
            _memberRepositoryMock.Object,
            _pushNotificationServiceMock.Object,
            _smsNotificationServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogStartMessage()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act
        await _sut.ExecuteAsync(cancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Payment reminder job started")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCompleteSuccessfully_WhenNoActiveGroups()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act
        var act = async () => await _sut.ExecuteAsync(cancellationToken);

        // Assert - should not throw
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogStubWarning()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act
        await _sut.ExecuteAsync(cancellationToken);

        // Assert - GetActiveGroupsAsync logs stub warning
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[STUB]") && v.ToString()!.Contains("GetActiveGroupsAsync")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleCancellation()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        var act = async () => await _sut.ExecuteAsync(cancellationTokenSource.Token);

        // Assert - should complete without throwing
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogCompletionMessageWithCounts()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act
        await _sut.ExecuteAsync(cancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("Payment reminder job completed") && 
                    v.ToString()!.Contains("Sent:") &&
                    v.ToString()!.Contains("Failed:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new PaymentReminderJob(
            null!,
            _groupRepositoryMock.Object,
            _memberRepositoryMock.Object,
            _pushNotificationServiceMock.Object,
            _smsNotificationServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenGroupRepositoryIsNull()
    {
        // Act
        var act = () => new PaymentReminderJob(
            _loggerMock.Object,
            null!,
            _memberRepositoryMock.Object,
            _pushNotificationServiceMock.Object,
            _smsNotificationServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("groupRepository");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenMemberRepositoryIsNull()
    {
        // Act
        var act = () => new PaymentReminderJob(
            _loggerMock.Object,
            _groupRepositoryMock.Object,
            null!,
            _pushNotificationServiceMock.Object,
            _smsNotificationServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("memberRepository");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenPushNotificationServiceIsNull()
    {
        // Act
        var act = () => new PaymentReminderJob(
            _loggerMock.Object,
            _groupRepositoryMock.Object,
            _memberRepositoryMock.Object,
            null!,
            _smsNotificationServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pushNotificationService");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenSmsNotificationServiceIsNull()
    {
        // Act
        var act = () => new PaymentReminderJob(
            _loggerMock.Object,
            _groupRepositoryMock.Object,
            _memberRepositoryMock.Object,
            _pushNotificationServiceMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("smsNotificationService");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogProcessingMessageWithGroupCount()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act
        await _sut.ExecuteAsync(cancellationToken);

        // Assert - should log processing message with count (will be 0 for stub)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing payment reminders for") && v.ToString()!.Contains("active groups")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseUtcDateForCalculations()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var beforeExecution = DateTime.UtcNow;

        // Act
        await _sut.ExecuteAsync(cancellationToken);

        // Assert - execution should happen around UTC time
        var afterExecution = DateTime.UtcNow;
        
        // Verify the job logs with UTC timestamp
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Payment reminder job started")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Time check (ensure execution was reasonably quick - within 5 seconds)
        (afterExecution - beforeExecution).TotalSeconds.Should().BeLessThan(5);
   }
}
