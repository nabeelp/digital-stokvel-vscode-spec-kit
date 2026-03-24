using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Core.ValueObjects;

namespace DigitalStokvel.Core.Entities;

/// <summary>
/// Represents a Stokvel group with its configuration and financial state
/// </summary>
public class StokvelsGroup : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    /// <summary>
    /// Group type: Savings, Burial, Investment, Grocery
    /// </summary>
    public string GroupType { get; set; } = "Savings";
    
    /// <summary>
    /// Fixed contribution amount per cycle (R50 - R100,000)
    /// </summary>
    public Money ContributionAmount { get; set; } = new Money(0);
    
    /// <summary>
    /// Contribution frequency: Weekly, Biweekly, Monthly
    /// </summary>
    public string ContributionFrequency { get; set; } = "Monthly";
    
    /// <summary>
    /// Group constitution stored as JSON (voting rules, payout policies, etc.)
    /// </summary>
    public string Constitution { get; set; } = "{}";
    
    /// <summary>
    /// Current group balance across all accounts
    /// </summary>
    public Money Balance { get; set; } = new Money(0);
    
    /// <summary>
    /// Group savings account number (from bank integration)
    /// </summary>
    public string? GroupSavingsAccountNumber { get; set; }
    
    /// <summary>
    /// Maximum members allowed (default: unlimited, soft warning at 50)
    /// </summary>
    public int? MaxMembers { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime? ClosedDate { get; set; }

    // Navigation properties
    public virtual ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();

    // Audit properties (from IAuditableEntity)
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
