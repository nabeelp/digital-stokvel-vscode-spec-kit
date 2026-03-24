using Microsoft.EntityFrameworkCore;
using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Infrastructure.Data;

namespace DigitalStokvel.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for StokvelsGroup entity with JSON constitution support
/// </summary>
public class GroupRepository : Repository<StokvelsGroup>, IGroupRepository
{
    public GroupRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<StokvelsGroup> CreateGroupAsync(StokvelsGroup group, CancellationToken cancellationToken = default)
    {
        await _context.Set<StokvelsGroup>().AddAsync(group, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return group;
    }

    public async Task<GroupMember> AddMemberAsync(Guid groupId, Guid memberId, string role = "Member", CancellationToken cancellationToken = default)
    {
        var groupMember = new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            MemberId = memberId,
            Role = role,
            JoinedDate = DateTime.UtcNow,
            IsActive = true
        };

        await _context.Set<GroupMember>().AddAsync(groupMember, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return groupMember;
    }

    public async Task AssignRoleAsync(Guid groupId, Guid memberId, string role, CancellationToken cancellationToken = default)
    {
        var groupMember = await _context.Set<GroupMember>()
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.MemberId == memberId && gm.IsActive, cancellationToken);

        if (groupMember != null)
        {
            groupMember.Role = role;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<StokvelsGroup?> GetGroupWithMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<StokvelsGroup>()
            .Include(g => g.Members)
                .ThenInclude(gm => gm.Member)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
    }

    public async Task<IEnumerable<StokvelsGroup>> GetMemberGroupsAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<GroupMember>()
            .Where(gm => gm.MemberId == memberId && gm.IsActive)
            .Select(gm => gm.Group)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetMemberCountAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<GroupMember>()
            .CountAsync(gm => gm.GroupId == groupId && gm.IsActive, cancellationToken);
    }

    public async Task<bool> HasRoleAsync(Guid groupId, Guid memberId, string role, CancellationToken cancellationToken = default)
    {
        return await _context.Set<GroupMember>()
            .AnyAsync(gm => gm.GroupId == groupId && gm.MemberId == memberId && gm.Role == role && gm.IsActive, cancellationToken);
    }
}
