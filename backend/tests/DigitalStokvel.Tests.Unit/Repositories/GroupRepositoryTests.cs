using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.ValueObjects;
using DigitalStokvel.Infrastructure.Data;
using DigitalStokvel.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DigitalStokvel.Tests.Unit.Repositories;

public class GroupRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GroupRepository _sut;

    public GroupRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _sut = new GroupRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CreateGroupAsync Tests

    [Fact]
    public async Task CreateGroupAsync_ShouldAddGroupToDatabase()
    {
        // Arrange
        var group = new StokvelsGroup
        {
            Id = Guid.NewGuid(),
            Name = "Ubuntu Stokvel",
            Description = "Community savings group",
            GroupType = "Savings",
            ContributionAmount = new Money(100.00m),
            ContributionFrequency = "Monthly",
            Balance = new Money(0m),
            Constitution = "{}",
            MaxMembers = 20,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };

        // Act
        var result = await _sut.CreateGroupAsync(group);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(group.Id);

        var savedGroup = await _context.StokvelsGroups.FindAsync(group.Id);
        savedGroup.Should().NotBeNull();
        savedGroup!.Name.Should().Be("Ubuntu Stokvel");
    }

    #endregion

    #region AddMemberAsync Tests

    [Fact]
    public async Task AddMemberAsync_ShouldAddGroupMemberRelationship()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var group = new StokvelsGroup
        {
            Id = groupId,
            Name = "Test Group",
            GroupType = "Savings",
            ContributionAmount = new Money(100m),
            Balance = new Money(0m),
            Constitution = "{}",
            MaxMembers = 20,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };

        var member = new Member
        {
            Id = memberId,
            ApplicationUserId = "auth0|test",
            PhoneNumber = "+27821234567",
            BankCustomerId = "CUST123",
            PreferredLanguage = "EN",
            FicaVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };

        await _context.StokvelsGroups.AddAsync(group);
        await _context.Members.AddAsync(member);
        await _context.SaveChangesAsync();

        // Act
        var groupMember = await _sut.AddMemberAsync(groupId, memberId, "Member");

        // Assert
        groupMember.Should().NotBeNull();
        groupMember.GroupId.Should().Be(groupId);
        groupMember.MemberId.Should().Be(memberId);
        groupMember.Role.Should().Be("Member");
    }

    #endregion

    #region AssignRoleAsync Tests

    [Fact]
    public async Task AssignRoleAsync_WithExistingMember_ShouldUpdateRole()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var groupMember = new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            MemberId = memberId,
            Role = "Member",
            JoinedDate = DateTime.UtcNow,
            IsActive = true
        };

        await _context.GroupMembers.AddAsync(groupMember);
        await _context.SaveChangesAsync();

        // Act
        await _sut.AssignRoleAsync(groupId, memberId, "Treasurer");

        // Assert
        var updatedGroupMember = await _context.GroupMembers.FindAsync(groupMember.Id);
        updatedGroupMember!.Role.Should().Be("Treasurer");
    }

    #endregion

    #region GetGroupWithMembersAsync Tests

    [Fact]
    public async Task GetGroupWithMembersAsync_ShouldIncludeMembersAndTheirDetails()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var member1Id = Guid.NewGuid();

        var group = new StokvelsGroup
        {
            Id = groupId,
            Name = "Ubuntu Stokvel",
            GroupType = "Savings",
            ContributionAmount = new Money(100m),
            Balance = new Money(500m),
            Constitution = "{}",
            MaxMembers = 20,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };

        var member1 = new Member
        {
            Id = member1Id,
            ApplicationUserId = "auth0|user1",
            PhoneNumber = "+27821234567",
            BankCustomerId = "CUST1",
            PreferredLanguage = "EN",
            FicaVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test"
        };

        var groupMember1 = new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            MemberId = member1Id,
            Role = "Chairperson",
            JoinedDate = DateTime.UtcNow,
            IsActive = true
        };

        await _context.StokvelsGroups.AddAsync(group);
        await _context.Members.AddAsync(member1);
        await _context.GroupMembers.AddAsync(groupMember1);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        // Act
        var result = await _sut.GetGroupWithMembersAsync(groupId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Ubuntu Stokvel");
        result.Members.Should().HaveCount(1);
    }

    #endregion

    #region GetMemberCountAsync Tests

    [Fact]
    public async Task GetMemberCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        var groupMember1 = new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            MemberId = Guid.NewGuid(),
            Role = "Member",
            JoinedDate = DateTime.UtcNow,
            IsActive = true
        };

        var groupMember2 = new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            MemberId = Guid.NewGuid(),
            Role = "Member",
            JoinedDate = DateTime.UtcNow,
            IsActive = true
        };

        await _context.GroupMembers.AddRangeAsync(groupMember1, groupMember2);
        await _context.SaveChangesAsync();

        // Act
        var count = await _sut.GetMemberCountAsync(groupId);

        // Assert
        count.Should().Be(2);
    }

    #endregion

    #region HasRoleAsync Tests

    [Fact]
    public async Task HasRoleAsync_WithMatchingRole_ShouldReturnTrue()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var groupMember = new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            MemberId = memberId,
            Role = "Chairperson",
            JoinedDate = DateTime.UtcNow,
            IsActive = true
        };

        await _context.GroupMembers.AddAsync(groupMember);
        await _context.SaveChangesAsync();

        // Act
        var hasRole = await _sut.HasRoleAsync(groupId, memberId, "Chairperson");

        // Assert
        hasRole.Should().BeTrue();
    }

    [Fact]
    public async Task HasRoleAsync_WithDifferentRole_ShouldReturnFalse()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var groupMember = new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            MemberId = memberId,
            Role = "Member",
            JoinedDate = DateTime.UtcNow,
            IsActive = true
        };

        await _context.GroupMembers.AddAsync(groupMember);
        await _context.SaveChangesAsync();

        // Act
        var hasRole = await _sut.HasRoleAsync(groupId, memberId, "Chairperson");

        // Assert
        hasRole.Should().BeFalse();
    }

    #endregion
}
