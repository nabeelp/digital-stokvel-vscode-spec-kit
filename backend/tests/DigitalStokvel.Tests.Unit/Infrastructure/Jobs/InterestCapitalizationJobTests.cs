using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Infrastructure.Jobs;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DigitalStokvel.Tests.Unit.Infrastructure.Jobs;

/// <summary>
/// Unit tests for InterestCapitalizationJob - monthly interest capitalization
/// </summary>
public class InterestCapitalizationJobTests
{
    private readonly Mock<ILogger<InterestCapitalizationJob>> _loggerMock;
    private readonly Mock<IGroupRepository> _groupRepositoryMock;
    private readonly Mock<IInterestService> _interestServiceMock;
    private readonly InterestCapitalizationJob _sut; // System Under Test

    public InterestCapitalizationJobTests()
    {
        _loggerMock = new Mock<ILogger<InterestCapitalizationJob>>();
        _groupRepositoryMock = new Mock<IGroupRepository>();
        _interestServiceMock = new Mock<IInterestService>();

        _sut = new InterestCapitalizationJob(
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Monthly interest capitalization job started")),
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("[STUB]") && v.ToString()!.Contains("InterestCapitalizationJob")),
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

        // Act - should complete without throwing
        var act = async () => await _sut.ExecuteAsync(cancellationTokenSource.Token);

        // Assert
        await act.Should().NotThrowAsync();
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Monthly interest capitalization completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new InterestCapitalizationJob(
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
        var act = () => new InterestCapitalizationJob(
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
        var act = () => new InterestCapitalizationJob(
            _loggerMock.Object,
            _groupRepositoryMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("interestService");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncludeSuccessAndFailureCountsInCompletionLog()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act
        await _sut.ExecuteAsync(cancellationToken);

        // Assert - verify completion log contains counts
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("Success:") && 
                    v.ToString()!.Contains("Failed:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
