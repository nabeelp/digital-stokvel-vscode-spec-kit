using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Core.ValueObjects;

namespace DigitalStokvel.Core.Entities;

/// <summary>
/// Represents a daily interest calculation for a stokvel group
/// </summary>
public class InterestCalculation : IAuditableEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Group for which interest is calculated
    /// </summary>
    public Guid GroupId { get; set; }
    
    /// <summary>
    /// Date of this calculation
    /// </summary>
    public DateTime CalculationDate { get; set; }
    
    /// <summary>
    /// Principal amount at the time of calculation
    /// </summary>
    public Money PrincipalAmount { get; set; } = new Money(0);
    
    /// <summary>
    /// Annual interest rate applied (as decimal, e.g., 0.035 for 3.5%)
    /// </summary>
    public decimal InterestRate { get; set; }
    
    /// <summary>
    /// Interest accrued on this calculation date
    /// </summary>
    public Money AccruedAmount { get; set; } = new Money(0);
    
    /// <summary>
    /// Interest tier at time of calculation (Tier1_3_5Pct, Tier2_4_5Pct, Tier3_5_5Pct)
    /// </summary>
    public string InterestTier { get; set; } = string.Empty;
    
    /// <summary>
    /// Number of days compounded (typically 1 for daily)
    /// </summary>
    public int DaysCompounded { get; set; } = 1;

    // Navigation properties
    public virtual StokvelsGroup? Group { get; set; }

    // Audit properties (from IAuditableEntity)
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
