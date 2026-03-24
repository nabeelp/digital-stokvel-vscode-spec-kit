using DigitalStokvel.Core.Entities;

namespace DigitalStokvel.Core.Interfaces;

/// <summary>
/// Repository interface for StokvelsGroup entity operations
/// </summary>
public interface IGroupRepository : IRepository<StokvelsGroup>
{
    /// <summary>
    /// Creates a new stokvel group with initial configuration
    /// </summary>
    Task<StokvelsGroup> CreateGroupAsync(StokvelsGroup group, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a member to an existing group with specified role
    /// </summary>
    Task<GroupMember> AddMemberAsync(Guid groupId, Guid memberId, string role = "Member", CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns or updates a member's role in a group
    /// </summary>
    Task AssignRoleAsync(Guid groupId, Guid memberId, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a group with all its members (roster)
    /// </summary>
    Task<StokvelsGroup?> GetGroupWithMembersAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active groups that a member belongs to
    /// </summary>
    Task<IEnumerable<StokvelsGroup>> GetMemberGroupsAsync(Guid memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the member count for a specific group
    /// </summary>
    Task<int> GetMemberCountAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a member has a specific role in a group
    /// </summary>
    Task<bool> HasRoleAsync(Guid groupId, Guid memberId, string role, CancellationToken cancellationToken = default);
}
