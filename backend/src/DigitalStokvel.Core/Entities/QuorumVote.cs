using DigitalStokvel.Core.Interfaces;

namespace DigitalStokvel.Core.Entities;

/// <summary>
/// Represents a vote proposal requiring quorum approval
/// </summary>
public class QuorumVote : IAuditableEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// Group conducting the vote
    /// </summary>
    public Guid GroupId { get; set; }
    public StokvelsGroup Group { get; set; } = null!;
    
    /// <summary>
    /// Type of proposal (e.g., "ConstitutionChange", "MemberRemoval", "PartialWithdrawal")
    /// </summary>
    public string ProposalType { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed proposal information stored as JSON
    /// </summary>
    public string ProposalDetails { get; set; } = "{}";
    
    /// <summary>
    /// Brief summary of the proposal
    /// </summary>
    public string Summary { get; set; } = string.Empty;
    
    /// <summary>
    /// Member who initiated the vote
    /// </summary>
    public Guid InitiatedBy { get; set; }
    public GroupMember InitiatedByMember { get; set; } = null!;
    
    ///  <summary>
    /// Number of votes in favor
    /// </summary>
    public int VotesFor { get; set; }
    
    /// <summary>
    /// Number of votes against
    /// </summary>
    public int VotesAgainst { get; set; }
    
    /// <summary>
    /// Number of members who abstained
    /// </summary>
    public int VotesAbstain { get; set; }
    
    /// <summary>
    /// Required number of votes for approval (based on quorum threshold)
    /// </summary>
    public int RequiredVotes { get; set; }
    
    /// <summary>
    /// Current status: Open, Approved, Rejected, Expired
    /// </summary>
    public string Status { get; set; } = "Open";
    
    /// <summary>
    /// Vote deadline (typically 48-72 hours)
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// When the vote was finalized
    /// </summary>
    public DateTime? FinalizedAt { get; set; }
    
    /// <summary>
    /// Individual vote records stored as JSON array
    /// Format: [{ "memberId": "...", "vote": "For/Against/Abstain", "timestamp": "..." }]
    /// </summary>
    public string VoteRecords { get; set; } = "[]";
    
    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}
