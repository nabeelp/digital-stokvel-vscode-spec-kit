using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Enums;

namespace DigitalStokvel.Core.Interfaces;

/// <summary>
/// Service interface for governance and dispute resolution
/// </summary>
public interface IGovernanceService
{
    /// <summary>
    /// Defines or updates a governance rule in the group constitution
    /// </summary>
    Task<(bool Success, Guid? RuleId, string? ErrorMessage)> DefineRuleAsync(
        Guid groupId,
        Guid memberId,
        RuleType ruleType,
        string ruleValue,
        string? description = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Initiates a vote requiring quorum approval
    /// </summary>
    Task<(bool Success, Guid? VoteId, string? ErrorMessage)> InitiateVoteAsync(
        Guid groupId,
        Guid initiatingMemberId,
        string proposalType,
        string proposalDetails,
        string summary,
        int durationHours = 72,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Casts a member's vote on a proposal
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> CastVoteAsync(
        Guid voteId,
        Guid memberId,
        string vote, // "For", "Against", "Abstain"
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Processes the final result of a vote (called when quorum reached or vote expires)
    /// </summary>
    Task<(bool Success, string Result, string? ErrorMessage)> ProcessVoteResultAsync(
        Guid voteId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Raises a dispute for Chairperson review
    /// </summary>
    Task<(bool Success, Guid? DisputeId, string? ErrorMessage)> RaiseDisputeAsync(
        Guid groupId,
        Guid memberId,
        string category,
        string description,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Chairperson resolves a dispute
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> ResolveDisputeAsync(
        Guid disputeId,
        Guid chairpersonId,
        string resolution,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Escalates unresolved dispute to bank mediation (after 7 days)
    /// </summary>
    Task<(bool Success, string? ErrorMessage)> EscalateDisputeToBankAsync(
        Guid disputeId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all governance rules for a group
    /// </summary>
    Task<IEnumerable<GovernanceRule>> GetGroupRulesAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets active votes for a group
    /// </summary>
    Task<IEnumerable<QuorumVote>> GetActiveVotesAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets open disputes for a group
    /// </summary>
    Task<IEnumerable<Dispute>> GetOpenDisputesAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);
}
