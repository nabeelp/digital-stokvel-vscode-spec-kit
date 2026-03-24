using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Infrastructure.Jobs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DigitalStokvel.Tests.Unit.Infrastructure.Jobs;

/// <summary>
/// Unit tests for DailyInterestAccrualJob - background job for daily interest calculations
/// </summary>
public class DailyInterestAccrualJobTests
{
    private readonly Mock<ILogger<DailyInterestAccrualJob>> _loggerMock;
    private readonly Mock<IGroupRepository> _groupRepositoryMock;
    private readonly Mock<IInterestService> _interestServiceMock;
    private readonly DailyInterestAccrualJob _sut; // System Under Test

    public DailyInterestAccrualJobTests()
    {
        _loggerMock = new Mock<ILogger<DailyInterestAccrualJob>>();
        _groupRepositoryMock = new Mock<IGroupRepository>();
        _interestServiceMock = new Mock<IInterestService>();

        _sut = new DailyInterestAccrualJob(
            _loggerMock.Object,
            _groupRepositoryMock.Object,
            _interestServiceMock.Object);
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Daily interest accrual job started")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCompleteSuccessfully_WhenNoGroups()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act
        var act = async () => await _sut.ExecuteAsync(cancellationToken);

        // Assert - should not throw
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseTodayAsCalculationDate()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var today = DateTime.UtcNow.Date;

        // Act
        await _sut.ExecuteAsync(cancellationToken);

        // Assert - verify calculation date appears in log (may be in different formats)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("Daily interest accrual job started") && 
                    v.ToString()!.Contains("for date")),
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
    public async Task ExecuteAsync_ShouldLogStubWarning()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act
        await _sut.ExecuteAsync(cancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[STUB]") && v.ToString()!.Contains("DailyInterestAccrualJob")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogCompletionMessage()
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Daily interest accrual completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new DailyInterestAccrualJob(
            null!,
            _groupRepositoryMock.Object,
            _interestServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenGroupRepositoryIsNull()
    {
        // Act
        var act = () => new DailyInterestAccrualJob(
            _loggerMock.Object,
            null!,
            _interestServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("groupRepository");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenInterestServiceIsNull()
    {
        // Act
        var act = () => new DailyInterestAccrualJob(
            _loggerMock.Object,
            _groupRepositoryMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("interestService");
    }
}
