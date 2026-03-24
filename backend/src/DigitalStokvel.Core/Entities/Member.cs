using DigitalStokvel.Core.Interfaces;

namespace DigitalStokvel.Core.Entities;

/// <summary>
/// Represents a member in the Digital Stokvel platform
/// </summary>
public class Member : IAuditableEntity
{
    public Guid Id { get; set; }
    public string ApplicationUserId { get; set; } = string.Empty; // Link to AspNetUsers
    public string PhoneNumber { get; set; } = string.Empty;
    public string? BankCustomerId { get; set; } // Link to banking system customer
    public string PreferredLanguage { get; set; } = "EN"; // EN, ZU, ST, XH, AF
    public bool FicaVerified { get; set; }
    public DateTime? FicaVerificationDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();

    // Audit properties (from IAuditableEntity)
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
