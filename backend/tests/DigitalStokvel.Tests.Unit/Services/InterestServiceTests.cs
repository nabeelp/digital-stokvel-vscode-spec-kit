using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Enums;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Core.ValueObjects;
using DigitalStokvel.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DigitalStokvel.Tests.Unit.Services;

/// <summary>
/// Unit tests for InterestService - tiered interest calculations with daily compounding
/// </summary>
public class InterestServiceTests
{
    private readonly Mock<IGroupRepository> _groupRepositoryMock;
    private readonly Mock<ILogger<InterestService>> _loggerMock;
    private readonly InterestService _sut; // System Under Test

    public InterestServiceTests()
    {
        _groupRepositoryMock = new Mock<IGroupRepository>();
        _loggerMock = new Mock<ILogger<InterestService>>();
        _sut = new InterestService(_groupRepositoryMock.Object, _loggerMock.Object);
    }

    #region DetermineInterestTier Tests

    [Theory]
    [InlineData(0, InterestTier.Tier1_3_5Pct)]
    [InlineData(5000, InterestTier.Tier1_3_5Pct)]
    [InlineData(9999.99, InterestTier.Tier1_3_5Pct)]
    [InlineData(10000, InterestTier.Tier2_4_5Pct)]
    [InlineData(25000, InterestTier.Tier2_4_5Pct)]
    [InlineData(49999.99, InterestTier.Tier2_4_5Pct)]
    [InlineData(50000, InterestTier.Tier3_5_5Pct)]
    [InlineData(100000, InterestTier.Tier3_5_5Pct)]
    [InlineData(1000000, InterestTier.Tier3_5_5Pct)]
    public void DetermineInterestTier_WithVariousBalances_ShouldReturnCorrectTier(decimal balance, InterestTier expectedTier)
    {
        // Act
        var tier = _sut.DetermineInterestTier(balance);

        // Assert
        tier.Should().Be(expectedTier);
    }

    [Theory]
    [InlineData(9999.99, InterestTier.Tier1_3_5Pct)] // Just below Tier 2
    [InlineData(10000, InterestTier.Tier2_4_5Pct)]   // Exact Tier 2 boundary
    [InlineData(10000.01, InterestTier.Tier2_4_5Pct)] // Just above Tier 2 boundary
    [InlineData(49999.99, InterestTier.Tier2_4_5Pct)] // Just below Tier 3
    [InlineData(50000, InterestTier.Tier3_5_5Pct)]   // Exact Tier 3 boundary
    [InlineData(50000.01, InterestTier.Tier3_5_5Pct)] // Just above Tier 3 boundary
    public void DetermineInterestTier_AtTierBoundaries_ShouldHandleEdgeCasesCorrectly(decimal balance, InterestTier expectedTier)
    {
        // Act
        var tier = _sut.DetermineInterestTier(balance);

        // Assert
        tier.Should().Be(expectedTier);
    }

    #endregion

    #region GetInterestRateForBalance Tests

    [Theory]
    [InlineData(0, 0.035)]          // Tier 1: 3.5%
    [InlineData(5000, 0.035)]       // Tier 1: 3.5%
    [InlineData(9999.99, 0.035)]    // Tier 1: 3.5%
    [InlineData(10000, 0.045)]      // Tier 2: 4.5%
    [InlineData(25000, 0.045)]      // Tier 2: 4.5%
    [InlineData(49999.99, 0.045)]   // Tier 2: 4.5%
    [InlineData(50000, 0.055)]      // Tier 3: 5.5%
    [InlineData(100000, 0.055)]     // Tier 3: 5.5%
    public void GetInterestRateForBalance_WithVariousBalances_ShouldReturnCorrectRate(decimal balance, decimal expectedRate)
    {
        // Act
        var rate = _sut.GetInterestRateForBalance(balance);

        // Assert
        rate.Should().Be(expectedRate);
    }

    #endregion

    #region CalculateDailyInterestAsync Tests

    [Fact]
    public async Task CalculateDailyInterestAsync_WithTier1Balance_ShouldCalculateInterestAt3_5Percent()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var calculationDate = new DateTime(2026, 3, 24);
        var principal = 5000m; // Tier 1

        var group = new StokvelsGroup
        {
            Id = groupId,
            Name = "Test Group",
            Balance = new Money(principal),
            GroupSavingsAccountNumber = "ACC123456"
        };

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId))
            .ReturnsAsync(group);

        // Expected: Daily rate = 3.5% / 365 = 0.00009589...
        // Interest = 5000 * (0.035 / 365) = 0.4794... ≈ 0.4795 (rounded to 4 decimals)
        var expectedInterest = 0.4795m;

        // Act
        var result = await _sut.CalculateDailyInterestAsync(groupId, calculationDate);

        // Assert
        result.Should().NotBeNull();
        result!.GroupId.Should().Be(groupId);
        result.CalculationDate.Should().Be(calculationDate.Date);
        result.PrincipalAmount.Amount.Should().Be(principal);
        result.InterestRate.Should().Be(0.035m);
        result.AccruedAmount.Amount.Should().BeApproximately(expectedInterest, 0.0001m);
        result.InterestTier.Should().Be(InterestTier.Tier1_3_5Pct.ToString());
        result.DaysCompounded.Should().Be(1);
    }

    [Fact]
    public async Task CalculateDailyInterestAsync_WithTier2Balance_ShouldCalculateInterestAt4_5Percent()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var calculationDate = new DateTime(2026, 3, 24);
        var principal = 25000m; // Tier 2

        var group = new StokvelsGroup
        {
            Id = groupId,
            Name = "Test Group",
            Balance = new Money(principal),
            GroupSavingsAccountNumber = "ACC123456"
        };

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId))
            .ReturnsAsync(group);

        // Expected: Daily rate = 4.5% / 365 = 0.00012328...
        // Interest = 25000 * (0.045 / 365) = 3.0822... ≈ 3.0822 (rounded to 4 decimals)
        var expectedInterest = 3.0822m;

        // Act
        var result = await _sut.CalculateDailyInterestAsync(groupId, calculationDate);

        // Assert
        result.Should().NotBeNull();
        result!.PrincipalAmount.Amount.Should().Be(principal);
        result.InterestRate.Should().Be(0.045m);
        result.AccruedAmount.Amount.Should().BeApproximately(expectedInterest, 0.0001m);
        result.InterestTier.Should().Be(InterestTier.Tier2_4_5Pct.ToString());
    }

    [Fact]
    public async Task CalculateDailyInterestAsync_WithTier3Balance_ShouldCalculateInterestAt5_5Percent()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var calculationDate = new DateTime(2026, 3, 24);
        var principal = 75000m; // Tier 3

        var group = new StokvelsGroup
        {
            Id = groupId,
            Name = "Test Group",
            Balance = new Money(principal),
            GroupSavingsAccountNumber = "ACC123456"
        };

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId))
            .ReturnsAsync(group);

        // Expected: Daily rate = 5.5% / 365 = 0.00015068...
        // Interest = 75000 * (0.055 / 365) = 11.3014... ≈ 11.3014 (rounded to 4 decimals)
        var expectedInterest = 11.3014m;

        // Act
        var result = await _sut.CalculateDailyInterestAsync(groupId, calculationDate);

        // Assert
        result.Should().NotBeNull();
        result!.PrincipalAmount.Amount.Should().Be(principal);
        result.InterestRate.Should().Be(0.055m);
        result.AccruedAmount.Amount.Should().BeApproximately(expectedInterest, 0.0001m);
        result.InterestTier.Should().Be(InterestTier.Tier3_5_5Pct.ToString());
    }

    [Fact]
    public async Task CalculateDailyInterestAsync_WithZeroBalance_ShouldReturnNull()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var calculationDate = new DateTime(2026, 3, 24);

        var group = new StokvelsGroup
        {
            Id = groupId,
            Name = "Test Group",
            Balance = new Money(0),
            GroupSavingsAccountNumber = "ACC123456"
        };

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId))
            .ReturnsAsync(group);

        // Act
        var result = await _sut.CalculateDailyInterestAsync(groupId, calculationDate);

        // Assert
        result.Should().BeNull();
    }

    // Note: Negative balance test removed - Money value object doesn't allow negative amounts

    [Fact]
    public async Task CalculateDailyInterestAsync_WithNonExistentGroup_ShouldReturnNull()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var calculationDate = new DateTime(2026, 3, 24);

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId))
            .ReturnsAsync((StokvelsGroup?)null);

        // Act
        var result = await _sut.CalculateDailyInterestAsync(groupId, calculationDate);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(10000, 0.045)] // Exact boundary to Tier 2
    [InlineData(9999.99, 0.035)] // Just below Tier 2 boundary
    [InlineData(10000.01, 0.045)] // Just above Tier 2 boundary
    [InlineData(50000, 0.055)] // Exact boundary to Tier 3
    [InlineData(49999.99, 0.045)] // Just below Tier 3 boundary
    [InlineData(50000.01, 0.055)] // Just above Tier 3 boundary
    public async Task CalculateDailyInterestAsync_AtTierBoundaries_ShouldUseCorrectRate(decimal balance, decimal expectedRate)
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var calculationDate = new DateTime(2026, 3, 24);

        var group = new StokvelsGroup
        {
            Id = groupId,
            Name = "Test Group",
            Balance = new Money(balance),
            GroupSavingsAccountNumber = "ACC123456"
        };

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId))
            .ReturnsAsync(group);

        // Act
        var result = await _sut.CalculateDailyInterestAsync(groupId, calculationDate);

        // Assert
        result.Should().NotBeNull();
        result!.InterestRate.Should().Be(expectedRate);
    }

    [Fact]
    public async Task CalculateDailyInterestAsync_ShouldRoundInterestTo4DecimalPlaces()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var calculationDate = new DateTime(2026, 3, 24);
        var principal = 123.45m; // Small amount to test rounding

        var group = new StokvelsGroup
        {
            Id = groupId,
            Name = "Test Group",
            Balance = new Money(principal),
            GroupSavingsAccountNumber = "ACC123456"
        };

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId))
            .ReturnsAsync(group);

        // Act
        var result = await _sut.CalculateDailyInterestAsync(groupId, calculationDate);

        // Assert
        result.Should().NotBeNull();
        
        // Check that the interest amount has at most 4 decimal places
        var interestAmount = result!.AccruedAmount.Amount;
        var roundedAmount = Math.Round(interestAmount, 4);
        interestAmount.Should().Be(roundedAmount);
    }

    [Fact]
    public async Task CalculateDailyInterestAsync_ShouldSetCalculationDateToDateOnly()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var calculationDateTime = new DateTime(2026, 3, 24, 14, 30, 45); // With time component

        var group = new StokvelsGroup
        {
            Id = groupId,
            Name = "Test Group",
            Balance = new Money(1000),
            GroupSavingsAccountNumber = "ACC123456"
        };

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId))
            .ReturnsAsync(group);

        // Act
        var result = await _sut.CalculateDailyInterestAsync(groupId, calculationDateTime);

        // Assert
        result.Should().NotBeNull();
        result!.CalculationDate.Should().Be(new DateTime(2026, 3, 24)); // Date only, no time
        result.CalculationDate.TimeOfDay.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task CalculateDailyInterestAsync_WhenRepositoryThrowsException_ShouldReturnNull()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var calculationDate = new DateTime(2026, 3, 24);

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _sut.CalculateDailyInterestAsync(groupId, calculationDate);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CapitalizeMonthlyAsync Tests

    [Fact]
    public async Task CapitalizeMonthlyAsync_WithValidGroup_ShouldReturnSuccess()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var currentBalance = 10000m;

        var group = new StokvelsGroup
        {
            Id = groupId,
            Name = "Test Group",
            Balance = new Money(currentBalance),
            GroupSavingsAccountNumber = "ACC123456"
        };

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId))
            .ReturnsAsync(group);

        // Act
        var result = await _sut.CapitalizeMonthlyAsync(groupId);

        // Assert
        result.Success.Should().BeTrue();
        result.NewBalance.Should().Be(currentBalance); // Stub returns current balance
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task CapitalizeMonthlyAsync_WithNonExistentGroup_ShouldReturnError()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId))
            .ReturnsAsync((StokvelsGroup?)null);

        // Act
        var result = await _sut.CapitalizeMonthlyAsync(groupId);

        // Assert
        result.Success.Should().BeFalse();
        result.NewBalance.Should().Be(0);
        result.ErrorMessage.Should().Be("Group not found");
    }

    [Fact]
    public async Task CapitalizeMonthlyAsync_WhenRepositoryThrowsException_ShouldReturnError()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _sut.CapitalizeMonthlyAsync(groupId);

        // Assert
        result.Success.Should().BeFalse();
        result.NewBalance.Should().Be(0);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetInterestBreakdownAsync Tests

    [Fact]
    public async Task GetInterestBreakdownAsync_ShouldReturnEmptyListAsStub()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var fromDate = new DateTime(2026, 1, 1);
        var toDate = new DateTime(2026, 3, 24);

        // Act
        var result = await _sut.GetInterestBreakdownAsync(groupId, fromDate, toDate);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // Stub implementation returns empty list
    }

    #endregion

    #region CalculateYearToDateEarningsAsync Tests

    [Fact]
    public async Task CalculateYearToDateEarningsAsync_WithValidGroup_ShouldReturnZeroAsStub()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        var group = new StokvelsGroup
        {
            Id = groupId,
            Name = "Test Group",
            Balance = new Money(10000),
            GroupSavingsAccountNumber = "ACC123456"
        };

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId))
            .ReturnsAsync(group);

        // Act
        var result = await _sut.CalculateYearToDateEarningsAsync(groupId);

        // Assert
        result.Should().Be(0); // Stub implementation returns 0
    }

    [Fact]
    public async Task CalculateYearToDateEarningsAsync_WithNonExistentGroup_ShouldReturnZero()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId))
            .ReturnsAsync((StokvelsGroup?)null);

        // Act
        var result = await _sut.CalculateYearToDateEarningsAsync(groupId);

        // Assert
        result.Should().Be(0);
    }

    #endregion

    #region Daily Compounding Formula Validation

    [Fact]
    public async Task CalculateDailyInterestAsync_ShouldImplementCorrectCompoundingFormula()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var calculationDate = new DateTime(2026, 3, 24);
        var principal = 10000m;
        var annualRate = 0.045m; // 4.5% for Tier 2

        var group = new StokvelsGroup
        {
            Id = groupId,
            Name = "Test Group",
            Balance = new Money(principal),
            GroupSavingsAccountNumber = "ACC123456"
        };

        _groupRepositoryMock
            .Setup(x => x.GetByIdAsync(groupId))
            .ReturnsAsync(group);

        // Formula: A = P(1 + r/365)^days, for daily calculation: days = 1
        // A = 10000 * (1 + 0.045/365)^1 = 10000 * 1.00012328767... = 10001.2328767...
        // Interest = A - P = 1.2328767... ≈ 1.2329 (rounded to 4 decimals)
        var expectedInterest = 1.2329m;

        // Act
        var result = await _sut.CalculateDailyInterestAsync(groupId, calculationDate);

        // Assert
        result.Should().NotBeNull();
        result!.AccruedAmount.Amount.Should().BeApproximately(expectedInterest, 0.0001m);
    }

    #endregion
}
