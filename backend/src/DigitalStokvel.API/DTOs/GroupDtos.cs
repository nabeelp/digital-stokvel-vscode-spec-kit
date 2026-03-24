namespace DigitalStokvel.API.DTOs;

/// <summary>
/// Request DTO for creating a new stokvel group
/// </summary>
public record CreateGroupRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string GroupType { get; init; } = "Savings"; // Savings, Burial, Investment, Grocery
    public decimal ContributionAmount { get; init; }
    public string ContributionFrequency { get; init; } = "Monthly"; // Weekly, Biweekly, Monthly
    public Dictionary<string, object>? ConstitutionRules { get; init; }
}

/// <summary>
/// Response DTO for group details
/// </summary>
public record GroupResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string GroupType { get; init; } = string.Empty;
    public decimal ContributionAmount { get; init; }
    public string ContributionFrequency { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public string? GroupSavingsAccountNumber { get; init; }
    public int MaxMembers { get; init; }
    public int CurrentMemberCount { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<GroupMemberResponse>? Members { get; init; }
    public Dictionary<string, object>? Constitution { get; init; }
}

/// <summary>
/// Response DTO for group member details
/// </summary>
public record GroupMemberResponse
{
    public Guid MemberId { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime JoinedDate { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>
/// Request DTO for inviting a member to a group
/// </summary>
public record InviteMemberRequest
{
    public string PhoneNumber { get; init; } = string.Empty;
}

/// <summary>
/// Request DTO for assigning a role to a group member
/// </summary>
public record AssignRoleRequest
{
    public Guid MemberId { get; init; }
    public string Role { get; init; } = string.Empty; // Chairperson, Treasurer, Secretary, Member
}

/// <summary>
/// Response DTO for successful group creation
/// </summary>
public record CreateGroupResponse
{
    public Guid GroupId { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public string Role { get; init; } = "Chairperson";
    public string GroupSavingsAccountNumber { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
