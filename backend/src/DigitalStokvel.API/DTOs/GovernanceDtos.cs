using DigitalStokvel.Core.Enums;

namespace DigitalStokvel.API.DTOs;

/// <summary>
/// Request DTO for defining a governance rule
/// </summary>
public record DefineRuleRequest
{
    public string RuleType { get; init; } = string.Empty; // MissedPaymentPenalty, GracePeriod, etc.
    public string RuleValue { get; init; } = string.Empty; // JSON configuration
    public string? Description { get; init; }
}

/// <summary>
/// Response DTO for governance rule
/// </summary>
public record GovernanceRuleResponse
{
    public Guid Id { get; init; }
    public string RuleType { get; init; } = string.Empty;
    public string RuleValue { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public Guid? ApprovedByVoteId { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Request DTO for initiating a vote
/// </summary>
public record InitiateVoteRequest
{
    public string ProposalType { get; init; } = string.Empty; // RuleChange, MemberRemoval, etc.
    public string ProposalDetails { get; init; } = string.Empty; // JSON details
    public string? Summary { get; init; }
    public int DurationHours { get; init; } = 72; // Default 72 hours
}

/// <summary>
/// Response DTO for vote proposal
/// </summary>
public record VoteResponse
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public string ProposalType { get; init; } = string.Empty;
    public string ProposalDetails { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public int VotesFor { get; init; }
    public int VotesAgainst { get; init; }
    public int VotesAbstain { get; init; }
    public int RequiredVotes { get; init; }
    public string Status { get; init; } = string.Empty; // Open, Approved, Rejected, Expired
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool HasVoted { get; init; } // Indicates if current user has voted
}

/// <summary>
/// Request DTO for casting a vote
/// </summary>
public record CastVoteRequest
{
    public string VoteChoice { get; init; } = string.Empty; // For, Against, Abstain
}

/// <summary>
/// Response DTO for dispute
/// </summary>
public record DisputeResponse
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty; // Open, ChairpersonReviewed, Resolved, EscalatedToBank
    public string? Resolution { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public DateTime? EscalatedAt { get; init; }
}

/// <summary>
/// Request DTO for raising a dispute
/// </summary>
public record RaiseDisputeRequest
{
    public string Category { get; init; } = string.Empty; // MissedPayment, UnauthorizedWithdrawal, etc.
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Response DTO for group constitution
/// </summary>
public record ConstitutionResponse
{
    public Guid GroupId { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public List<GovernanceRuleResponse> Rules { get; init; } = new();
    public DateTime LastUpdated { get; init; }
}
