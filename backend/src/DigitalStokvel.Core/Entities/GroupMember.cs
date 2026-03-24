using DigitalStokvel.Core.Interfaces;

namespace DigitalStokvel.Core.Entities;

/// <summary>
/// Join table representing a member's participation in a specific group
/// </summary>
public class GroupMember : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid MemberId { get; set; }
    
    /// <summary>
    /// Member's role in the group: Chairperson, Treasurer, Secretary, Member
    /// </summary>
    public string Role { get; set; } = "Member";
    
    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public DateTime? LeftDate { get; set; }
    public string? LeaveReason { get; set; }

    // Navigation properties
    public virtual StokvelsGroup Group { get; set; } = null!;
    public virtual Member Member { get; set; } = null!;

    // Audit properties (from IAuditableEntity)
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
