namespace DigitalStokvel.Core.Enums;

/// <summary>
/// Status of a dispute resolution workflow
/// </summary>
public enum DisputeStatus
{
    /// <summary>
    /// Dispute has been reported and awaits Chairperson review
    /// </summary>
    Open = 1,
    
    /// <summary>
    /// Chairperson has reviewed and is working on resolution
    /// </summary>
    ChairpersonReviewed = 2,
    
    /// <summary>
    /// Dispute resolved internally within the group
    /// </summary>
    Resolved = 3,
    
    /// <summary>
    /// Dispute escalated to bank for mediation (after 7 days unresolved)
    /// </summary>
    EscalatedToBank = 4,
    
    /// <summary>
    /// Dispute closed without resolution (member withdrew complaint)
    /// </summary>
    Withdrawn = 5
}
