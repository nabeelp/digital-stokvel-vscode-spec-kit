using DigitalStokvel.Core.Enums;
using DigitalStokvel.Core.Interfaces;

namespace DigitalStokvel.Core.Entities;

/// <summary>
/// Represents a dispute raised by a member regarding group operations
/// </summary>
public class Dispute : IAuditableEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Group where the dispute occurred
    /// </summary>
    public Guid GroupId { get; set; }
    public StokvelsGroup Group { get; set; } = null!;
    
    /// <summary>
    /// Member who raised the dispute
    /// </summary>
    public Guid RaisedBy { get; set; }
    public GroupMember RaisedByMember { get; set; } = null!;
    
    /// <summary>
    /// Category of dispute (e.g., "Missed Payment", "Payout Delay", "Rule Violation")
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description of the dispute
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status of the dispute
    /// </summary>
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    
    /// <summary>
    /// Resolution provided by Chairperson or bank mediator
    /// </summary>
    public string? Resolution { get; set; }
    
    /// <summary>
    /// When the dispute was escalated to bank mediation
    /// </summary>
    public DateTime? EscalatedAt { get; set; }
    
    /// <summary>
    /// Bank staff member handling the escalation
    /// </summary>
    public string? BankMediatorId { get; set; }
    
    /// <summary>
    /// When the dispute was resolved
    /// </summary>
    public DateTime? ResolvedAt { get; set; }
    
    /// <summary>
    /// Member who resolved the dispute (Chairperson or bank mediator)
    /// </summary>
    public Guid? ResolvedBy { get; set; }
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
