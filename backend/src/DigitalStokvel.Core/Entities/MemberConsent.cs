using DigitalStokvel.Core.Interfaces;

namespace DigitalStokvel.Core.Entities;

/// <summary>
/// Represents a member's consent record for POPIA compliance
/// </summary>
public class MemberConsent : IAuditableEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to Member
    /// </summary>
    public Guid MemberId { get; set; }

    /// <summary>
    /// Type of consent (e.g., CreditBureau, Marketing, DataProcessing)
    /// </summary>
    public string ConsentType { get; set; } = string.Empty;

    /// <summary>
    /// Whether consent was given or denied
    /// </summary>
    public bool ConsentGiven { get; set; }

    /// <summary>
    /// When the consent was recorded
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional expiry date for time-limited consents
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// IP address from which consent was given (audit trail)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string (audit trail)
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Navigation property to Member
    /// </summary>
    public virtual Member? Member { get; set; }

    // IAuditableEntity implementation
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
