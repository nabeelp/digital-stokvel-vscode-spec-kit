namespace DigitalStokvel.Core.Interfaces;

/// <summary>
/// Base interface for entities that require audit tracking.
/// Automatically populated by SaveChangesInterceptor on create/update.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// When the entity was created (UTC)
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// User ID who created the entity
    /// </summary>
    string CreatedBy { get; set; }

    /// <summary>
    /// When the entity was last modified (UTC)
    /// </summary>
    DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User ID who last modified the entity
    /// </summary>
    string? ModifiedBy { get; set; }
}
