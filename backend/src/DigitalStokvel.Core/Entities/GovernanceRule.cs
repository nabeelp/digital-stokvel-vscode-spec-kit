using DigitalStokvel.Core.Enums;
using DigitalStokvel.Core.Interfaces;

namespace DigitalStokvel.Core.Entities;

/// <summary>
/// Represents a governance rule defined in a group's constitution
/// </summary>
public class GovernanceRule : IAuditableEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Group this rule applies to
    /// </summary>
    public Guid GroupId { get; set; }
    public StokvelsGroup Group { get; set; } = null!;
    
    /// <summary>
    /// Type of rule (MissedPaymentPenalty, GracePeriod, etc.)
    /// </summary>
    public RuleType RuleType { get; set; }
    
    /// <summary>
    /// Rule configuration stored as JSON
    /// Examples:
    /// - MissedPaymentPenalty: { "amount": 50, "type": "fixed" } or { "percentage": 10, "type": "percentage" }
    /// - GracePeriod: { "days": 3 }
    /// - MemberRemovalCriteria: { "missedPaymentsThreshold": 3 }
    /// - QuorumThreshold: { "percentage": 60 }
    /// </summary>
    public string RuleValue { get; set; } = "{}";
    
    /// <summary>
    /// Human-readable description of the rule
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Whether this rule is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Vote ID that approved this rule (if applicable)
    /// </summary>
    public Guid? ApprovedByVoteId { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
