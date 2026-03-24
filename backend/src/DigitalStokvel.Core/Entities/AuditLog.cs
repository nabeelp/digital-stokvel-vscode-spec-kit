namespace DigitalStokvel.Core.Entities;

/// <summary>
/// Audit log entry for tracking all data changes (POPIA & FICA compliance)
/// Retained for 7 years per regulatory requirements
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }

    /// <summary>
    /// Entity type being audited (e.g., "Member", "Group", "Contribution")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity being audited
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Action performed (e.g., "Create", "Update", "Delete", "Read")
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Previous value of the entity (JSON) - null for Create actions
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// New value of the entity (JSON) - null for Delete actions
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// User ID who performed the action
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Member ID if applicable (for user-initiated actions)
    /// </summary>
    public Guid? MemberId { get; set; }

    /// <summary>
    /// When the action occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address from which the action was performed
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// HTTP method (GET, POST, PUT, DELETE)
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Request path
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// Additional context or reason for the action
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Whether this log has been archived to long-term storage
    /// </summary>
    public bool IsArchived { get; set; } = false;

    /// <summary>
    /// Date when this log should be archived (typically 1 year after creation)
    /// </summary>
    public DateTime? ArchiveDate { get; set; }
}
