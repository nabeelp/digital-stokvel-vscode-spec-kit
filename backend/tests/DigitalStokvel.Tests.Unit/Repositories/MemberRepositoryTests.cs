using DigitalStokvel.Core.Entities;
using DigitalStokvel.Infrastructure.Data;
using DigitalStokvel.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalStokvel.Tests.Unit.Repositories;

public class MemberRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly MemberRepository _sut;

    public MemberRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _sut = new MemberRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetByPhoneNumberAsync Tests

    [Fact]
    public async Task GetByPhoneNumberAsync_WithExistingPhone_ShouldReturnMember()
    {
        // Arrange
        var member = new Member
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = "auth0|user123",
            PhoneNumber = "+27821234567",
            BankCustomerId = "CUST123",
            PreferredLanguage = "EN",
            FicaVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        await _context.Members.AddAsync(member);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByPhoneNumberAsync("+27821234567");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(member.Id);
        result.PhoneNumber.Should().Be("+27821234567");
    }

    [Fact]
    public async Task GetByPhoneNumberAsync_WithNonExistentPhone_ShouldReturnNull()
    {
        // Act
        var result = await _sut.GetByPhoneNumberAsync("+27829999999");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByBankCustomerIdAsync Tests

    [Fact]
    public async Task GetByBankCustomerIdAsync_WithExistingCustomerId_ShouldReturnMember()
    {
        // Arrange
        var member = new Member
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = "auth0|user456",
            PhoneNumber = "+27821234567",
            BankCustomerId = "CUST456",
            PreferredLanguage = "ZU",
            FicaVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        await _context.Members.AddAsync(member);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByBankCustomerIdAsync("CUST456");

        // Assert
        result.Should().NotBeNull();
        result!.BankCustomerId.Should().Be("CUST456");
    }

    #endregion

    #region GetByApplicationUserIdAsync Tests

    [Fact]
    public async Task GetByApplicationUserIdAsync_WithExistingUserId_ShouldReturnMember()
    {
        // Arrange
        var userId = "auth0|12345";
        var member = new Member
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = userId,
            PhoneNumber = "+27821234567",
            BankCustomerId = "CUST789",
            PreferredLanguage = "EN",
            FicaVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };
        await _context.Members.AddAsync(member);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByApplicationUserIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.ApplicationUserId.Should().Be(userId);
    }

    #endregion

    #region GetVerifiedMembersAsync Tests

    [Fact]
    public async Task GetVerifiedMembersAsync_ShouldReturnOnlyVerifiedAndActiveMembers()
    {
        // Arrange
        var verifiedMember1 = new Member
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = "user1",
            PhoneNumber = "+27821234567",
            BankCustomerId = "CUST1",
            PreferredLanguage = "EN",
            FicaVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };

        var verifiedMember2 = new Member
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = "user2",
            PhoneNumber = "+27821234568",
            BankCustomerId = "CUST2",
            PreferredLanguage = "ZU",
            FicaVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };

        var unverifiedMember = new Member
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = "user3",
            PhoneNumber = "+27821234569",
            BankCustomerId = "CUST3",
            PreferredLanguage = "EN",
            FicaVerified = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };

        await _context.Members.AddRangeAsync(verifiedMember1, verifiedMember2, unverifiedMember);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetVerifiedMembersAsync();

        // Assert
        var membersList = result.ToList();
        membersList.Should().HaveCount(2);
        membersList.Should().Contain(m => m.Id == verifiedMember1.Id);
        membersList.Should().Contain(m => m.Id == verifiedMember2.Id);
    }

    #endregion
}
