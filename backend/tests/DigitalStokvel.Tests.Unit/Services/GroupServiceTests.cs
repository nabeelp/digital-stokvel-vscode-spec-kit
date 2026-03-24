using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Core.ValueObjects;
using DigitalStokvel.Infrastructure.Messaging;
using DigitalStokvel.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DigitalStokvel.Tests.Unit.Services;

public class GroupServiceTests
{
    private readonly Mock<IGroupRepository> _groupRepositoryMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly Mock<IServiceBusClient> _serviceBusClientMock;
    private readonly Mock<ILogger<GroupService>> _loggerMock;
    private readonly GroupService _sut; // System Under Test

    public GroupServiceTests()
    {
        _groupRepositoryMock = new Mock<IGroupRepository>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _serviceBusClientMock = new Mock<IServiceBusClient>();
        _loggerMock = new Mock<ILogger<GroupService>>();

        _sut = new GroupService(
            _groupRepositoryMock.Object,
            _memberRepositoryMock.Object,
            _localizationServiceMock.Object,
            _serviceBusClientMock.Object,
            _loggerMock.Object);
    }

    #region CreateGroupAsync Tests

    [Fact]
    public async Task CreateGroupAsync_WithValidData_ShouldCreateGroupSuccessfully()
    {
        // Arrange
        var creatorMemberId = Guid.NewGuid();
        var creatorMember = new Member
        {
            Id = creatorMemberId,
            PhoneNumber = "+27821234567",
            FicaVerified = true,
            PreferredLanguage = "en"
        };

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(creatorMemberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creatorMember);

        _groupRepositoryMock
            .Setup(x => x.CreateGroupAsync(It.IsAny<StokvelsGroup>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StokvelsGroup g, CancellationToken ct) => g);

        _groupRepositoryMock
            .Setup(x => x.AddMemberAsync(It.IsAny<Guid>(), creatorMemberId, "Chairperson", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroupMember 
            { 
                GroupId = It.IsAny<Guid>(), 
                MemberId = creatorMemberId, 
                Role = "Chairperson" 
            });

        // Act
        var result = await _sut.CreateGroupAsync(
            creatorMemberId,
            "Ntombizodwa Stokvel",
            "Monthly grocery savings",
            "Grocery",
            500.00m,
            "Monthly");

        // Assert
        result.Success.Should().BeTrue();
        result.Group.Should().NotBeNull();
        result.Group!.Name.Should().Be("Ntombizodwa Stokvel");
        result.Group.ContributionAmount.Amount.Should().Be(500.00m);
        result.Group.GroupSavingsAccountNumber.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();

        _groupRepositoryMock.Verify(x => x.AddMemberAsync(
            It.IsAny<Guid>(),
            creatorMemberId,
            "Chairperson",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateGroupAsync_WithNonExistentMember_ShouldReturnError()
    {
        // Arrange
        var creatorMemberId = Guid.NewGuid();

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(creatorMemberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        // Act
        var result = await _sut.CreateGroupAsync(
            creatorMemberId,
            "Test Group",
            null,
            "Savings",
            500.00m,
            "Monthly");

        // Assert
        result.Success.Should().BeFalse();
        result.Group.Should().BeNull();
        result.ErrorMessage.Should().Be("Member not found");
    }

    [Fact]
    public async Task CreateGroupAsync_WithNonFicaVerifiedMember_ShouldReturnError()
    {
        // Arrange
        var creatorMemberId = Guid.NewGuid();
        var creatorMember = new Member
        {
            Id = creatorMemberId,
            PhoneNumber = "+27821234567",
            FicaVerified = false,
            PreferredLanguage = "en"
        };

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(creatorMemberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creatorMember);

        _localizationServiceMock
            .Setup(x => x.GetString("error.fica_not_verified", "en"))
            .Returns("FICA verification required");

        // Act
        var result = await _sut.CreateGroupAsync(
            creatorMemberId,
            "Test Group",
            null,
            "Savings",
            500.00m,
            "Monthly");

        // Assert
        result.Success.Should().BeFalse();
        result.Group.Should().BeNull();
        result.ErrorMessage.Should().Contain("FICA");
    }

    [Theory]
    [InlineData(49.99)] // Below minimum
    [InlineData(100000.01)] // Above maximum
    public async Task CreateGroupAsync_WithInvalidContributionAmount_ShouldReturnError(decimal amount)
    {
        // Arrange
        var creatorMemberId = Guid.NewGuid();
        var creatorMember = new Member
        {
            Id = creatorMemberId,
            PhoneNumber = "+27821234567",
            FicaVerified = true,
            PreferredLanguage = "en"
        };

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(creatorMemberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creatorMember);

        _localizationServiceMock
            .Setup(x => x.GetString("error.invalid_contribution_amount", "en", 50.00m, 100000.00m))
            .Returns("Contribution must be between R50 and R100,000");

        // Act
        var result = await _sut.CreateGroupAsync(
            creatorMemberId,
            "Test Group",
            null,
            "Savings",
            amount,
            "Monthly");

        // Assert
        result.Success.Should().BeFalse();
        result.Group.Should().BeNull();
        result.ErrorMessage.Should().Contain("R50");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("AB")] // Too short (less than 3 characters)
    public async Task CreateGroupAsync_WithInvalidGroupName_ShouldReturnError(string name)
    {
        // Arrange
        var creatorMemberId = Guid.NewGuid();
        var creatorMember = new Member
        {
            Id = creatorMemberId,
            PhoneNumber = "+27821234567",
            FicaVerified = true,
            PreferredLanguage = "en"
        };

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(creatorMemberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(creatorMember);

        _localizationServiceMock
            .Setup(x => x.GetString("error.invalid_group_name", "en"))
            .Returns("Group name must be at least 3 characters");

        // Act
        var result = await _sut.CreateGroupAsync(
            creatorMemberId,
            name,
            null,
            "Savings",
            500.00m,
            "Monthly");

        // Assert
        result.Success.Should().BeFalse();
        result.Group.Should().BeNull();
        result.ErrorMessage.Should().Contain("name");
    }

    #endregion

    #region InviteMemberAsync Tests

    [Fact]
    public async Task InviteMemberAsync_WithValidDataByChairperson_ShouldSendInvitation()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var inviterMemberId = Guid.NewGuid();
        var inviteePhone = "+27821112222";

        var group = new StokvelsGroup
        {
            Id = groupId,
            Name = "Test Group",
            IsActive = true,
            MaxMembers = 50,
            ContributionAmount = new Money(500, "ZAR"),
            ContributionFrequency = "Monthly",
            Members = new List<GroupMember>()
        };

        var inviter = new Member
        {
            Id = inviterMemberId,
            PhoneNumber = "+27821234567",
            PreferredLanguage = "en"
        };

        _groupRepositoryMock
            .Setup(x => x.GetGroupWithMembersAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        _groupRepositoryMock
            .Setup(x => x.HasRoleAsync(groupId, inviterMemberId, "Chairperson", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(inviterMemberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);

        _memberRepositoryMock
            .Setup(x => x.GetByPhoneNumberAsync(inviteePhone, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        _groupRepositoryMock
            .Setup(x => x.GetMemberCountAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        _serviceBusClientMock
            .Setup(x => x.SendNotificationAsync(
                It.IsAny<string>(),
                "GroupInvitation",
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.InviteMemberAsync(groupId, inviterMemberId, inviteePhone);

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();

        _serviceBusClientMock.Verify(x => x.SendNotificationAsync(
            It.IsAny<string>(),
            "GroupInvitation",
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InviteMemberAsync_WithNonExistentGroup_ShouldReturnError()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var inviterMemberId = Guid.NewGuid();

        _groupRepositoryMock
            .Setup(x => x.GetGroupWithMembersAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StokvelsGroup?)null);

        // Act
        var result = await _sut.InviteMemberAsync(groupId, inviterMemberId, "+27821112222");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task InviteMemberAsync_ByMemberWithoutPermission_ShouldReturnError()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var inviterMemberId = Guid.NewGuid();

        var group = new StokvelsGroup
        {
            Id = groupId,
            IsActive = true,
            Members = new List<GroupMember>()
        };

        _groupRepositoryMock
            .Setup(x => x.GetGroupWithMembersAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        _groupRepositoryMock
            .Setup(x => x.HasRoleAsync(groupId, inviterMemberId, "Chairperson", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _groupRepositoryMock
            .Setup(x => x.HasRoleAsync(groupId, inviterMemberId, "Secretary", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.InviteMemberAsync(groupId, inviterMemberId, "+27821112222");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Chairperson or Secretary");
    }

    [Fact]
    public async Task InviteMemberAsync_WhenMemberAlreadyInGroup_ShouldReturnError()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var inviterMemberId = Guid.NewGuid();
        var inviteeId = Guid.NewGuid();
        var inviteePhone = "+27821112222";

        var invitee = new Member { Id = inviteeId, PhoneNumber = inviteePhone };

        var group = new StokvelsGroup
        {
            Id = groupId,
            IsActive = true,
            Members = new List<GroupMember>
            {
                new() { MemberId = inviteeId, IsActive = true }
            }
        };

        _groupRepositoryMock
            .Setup(x => x.GetGroupWithMembersAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        _groupRepositoryMock
            .Setup(x => x.HasRoleAsync(groupId, inviterMemberId, "Chairperson", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(inviterMemberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { Id = inviterMemberId, PreferredLanguage = "en" });

        _memberRepositoryMock
            .Setup(x => x.GetByPhoneNumberAsync(inviteePhone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitee);

        // Act
        var result = await _sut.InviteMemberAsync(groupId, inviterMemberId, inviteePhone);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already part of the group");
    }

    [Fact]
    public async Task InviteMemberAsync_WhenGroupAtCapacity_ShouldReturnError()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var inviterMemberId = Guid.NewGuid();

        var group = new StokvelsGroup
        {
            Id = groupId,
            IsActive = true,
            MaxMembers = 50,
            Members = new List<GroupMember>()
        };

        var inviter = new Member { Id = inviterMemberId, PreferredLanguage = "en" };

        _groupRepositoryMock
            .Setup(x => x.GetGroupWithMembersAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        _groupRepositoryMock
            .Setup(x => x.HasRoleAsync(groupId, inviterMemberId, "Chairperson", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(inviterMemberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);

        _memberRepositoryMock
            .Setup(x => x.GetByPhoneNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        _groupRepositoryMock
            .Setup(x => x.GetMemberCountAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(50); // At capacity

        _localizationServiceMock
            .Setup(x => x.GetString("warning.group_at_capacity", "en", 50))
            .Returns("Group has reached maximum capacity of 50 members");

        // Act
        var result = await _sut.InviteMemberAsync(groupId, inviterMemberId, "+27821112222");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("capacity");
    }

    #endregion

    #region AssignRoleAsync Tests

    [Fact]
    public async Task AssignRoleAsync_WithValidData_ShouldAssignRole()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var assignerMemberId = Guid.NewGuid();
        var targetMemberId = Guid.NewGuid();

        var group = new StokvelsGroup
        {
            Id = groupId,
            IsActive = true,
            Members = new List<GroupMember>
            {
                new() { MemberId = targetMemberId, IsActive = true, Role = "Member" }
            }
        };

        _groupRepositoryMock
            .Setup(x => x.GetGroupWithMembersAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        _groupRepositoryMock
            .Setup(x => x.HasRoleAsync(groupId, assignerMemberId, "Chairperson", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _groupRepositoryMock
            .Setup(x => x.AssignRoleAsync(groupId, targetMemberId, "Secretary", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.AssignRoleAsync(groupId, assignerMemberId, targetMemberId, "Secretary");

        // Assert
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();

        _groupRepositoryMock.Verify(x => x.AssignRoleAsync(
            groupId,
            targetMemberId,
            "Secretary",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignRoleAsync_ByNonChairperson_ShouldReturnError()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var assignerMemberId = Guid.NewGuid();
        var targetMemberId = Guid.NewGuid();

        var group = new StokvelsGroup
        {
            Id = groupId,
            IsActive = true,
            Members = new List<GroupMember>()
        };

        _groupRepositoryMock
            .Setup(x => x.GetGroupWithMembersAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        _groupRepositoryMock
            .Setup(x => x.HasRoleAsync(groupId, assignerMemberId, "Chairperson", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.AssignRoleAsync(groupId, assignerMemberId, targetMemberId, "Treasurer");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Only the Chairperson");
    }

    [Fact]
    public async Task AssignRoleAsync_WithChairpersonRole_ShouldReturnError()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var assignerMemberId = Guid.NewGuid();
        var targetMemberId = Guid.NewGuid();

        var group = new StokvelsGroup
        {
            Id = groupId,
            IsActive = true,
            Members = new List<GroupMember>
            {
                new() { MemberId = targetMemberId, IsActive = true }
            }
        };

        _groupRepositoryMock
            .Setup(x => x.GetGroupWithMembersAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        _groupRepositoryMock
            .Setup(x => x.HasRoleAsync(groupId, assignerMemberId, "Chairperson", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.AssignRoleAsync(groupId, assignerMemberId, targetMemberId, "Chairperson");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cannot assign Chairperson role");
    }

    [Fact]
    public async Task AssignRoleAsync_WhenTreasurerAlreadyExists_ShouldReturnError()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var assignerMemberId = Guid.NewGuid();
        var targetMemberId = Guid.NewGuid();
        var existingTreasurerId = Guid.NewGuid();

        var group = new StokvelsGroup
        {
            Id = groupId,
            IsActive = true,
            Members = new List<GroupMember>
            {
                new() { MemberId = existingTreasurerId, IsActive = true, Role = "Treasurer" },
                new() { MemberId = targetMemberId, IsActive = true, Role = "Member" }
            }
        };

        _groupRepositoryMock
            .Setup(x => x.GetGroupWithMembersAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        _groupRepositoryMock
            .Setup(x => x.HasRoleAsync(groupId, assignerMemberId, "Chairperson", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.AssignRoleAsync(groupId, assignerMemberId, targetMemberId, "Treasurer");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("only be one Treasurer");
    }

    [Fact]
    public async Task AssignRoleAsync_TreasurerWithoutFicaVerification_ShouldReturnError()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var assignerMemberId = Guid.NewGuid();
        var targetMemberId = Guid.NewGuid();

        var group = new StokvelsGroup
        {
            Id = groupId,
            IsActive = true,
            Members = new List<GroupMember>
            {
                new() { MemberId = targetMemberId, IsActive = true, Role = "Member" }
            }
        };

        var targetMember = new Member
        {
            Id = targetMemberId,
            FicaVerified = false
        };

        _groupRepositoryMock
            .Setup(x => x.GetGroupWithMembersAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        _groupRepositoryMock
            .Setup(x => x.HasRoleAsync(groupId, assignerMemberId, "Chairperson", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _memberRepositoryMock
            .Setup(x => x.GetByIdAsync(targetMemberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetMember);

        // Act
        var result = await _sut.AssignRoleAsync(groupId, assignerMemberId, targetMemberId, "Treasurer");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("FICA verified");
    }

    [Theory]
    [InlineData("InvalidRole")]
    [InlineData("")]
    [InlineData("Admin")]
    public async Task AssignRoleAsync_WithInvalidRole_ShouldReturnError(string invalidRole)
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var assignerMemberId = Guid.NewGuid();
        var targetMemberId = Guid.NewGuid();

        var group = new StokvelsGroup { Id = groupId, IsActive = true };

        _groupRepositoryMock
            .Setup(x => x.GetGroupWithMembersAsync(groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        // Act
        var result = await _sut.AssignRoleAsync(groupId, assignerMemberId, targetMemberId, invalidRole);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid role");
    }

    #endregion
}
