using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DigitalStokvel.API.DTOs;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Core.Enums;

namespace DigitalStokvel.API.Controllers;

[ApiController]
[Route("api/v1/groups/{groupId}/[controller]")]
[Authorize]
public class GovernanceController : ControllerBase
{
    private readonly IGovernanceService _governanceService;
    private readonly IMemberRepository _memberRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly ILogger<GovernanceController> _logger;

    public GovernanceController(
        IGovernanceService governanceService,
        IMemberRepository memberRepository,
        IGroupRepository groupRepository,
        ILogger<GovernanceController> logger)
    {
        _governanceService = governanceService ?? throw new ArgumentNullException(nameof(governanceService));
        _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Define or update a governance rule (Chairperson only)
    /// </summary>
    /// <remarks>
    /// Allows Chairperson to define or update group governance rules.
    /// Rule changes deactivate previous versions and create new ones.
    /// Example rule types: MissedPaymentPenalty, GracePeriod, QuorumThreshold, etc.
    /// </remarks>
    [HttpPost("constitution")]
    [ProducesResponseType(typeof(ApiResponse<GovernanceRuleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DefineRule(
        [FromRoute] Guid groupId,
        [FromBody] DefineRuleRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ErrorResponse
            {
                Message = "User not authenticated",
                ErrorCode = "AUTHENTICATION_REQUIRED"
            });
        }

        // Get member by ApplicationUserId
        var member = await _memberRepository.GetByApplicationUserIdAsync(userId);
        if (member == null)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Member profile not found",
                ErrorCode = "MEMBER_NOT_FOUND"
            });
        }

        // Validate group exists
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
        {
            return NotFound(new ErrorResponse
            {
                Message = "Group not found",
                ErrorCode = "GROUP_NOT_FOUND"
            });
        }

        // Parse RuleType from string
        if (!Enum.TryParse<RuleType>(request.RuleType, out var ruleType))
        {
            return BadRequest(new ErrorResponse
            {
                Message = $"Invalid rule type: {request.RuleType}",
                ErrorCode = "INVALID_RULE_TYPE"
            });
        }

        // Validate JSON format for RuleValue
        try
        {
            JsonDocument.Parse(request.RuleValue);
        }
        catch
        {
            return BadRequest(new ErrorResponse
            {
                Message = "RuleValue must be valid JSON",
                ErrorCode = "INVALID_JSON_FORMAT"
            });
        }

        var (success, ruleId, errorMessage) = await _governanceService.DefineRuleAsync(
            groupId,
            member.Id,
            ruleType,
            request.RuleValue,
            request.Description);

        if (!success || !ruleId.HasValue)
        {
            _logger.LogWarning("Rule definition failed for Group {GroupId} by Member {MemberId}: {Error}",
                groupId, member.Id, errorMessage);

            var statusCode = errorMessage?.Contains("Chairperson") == true
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;

            return StatusCode(statusCode, new ErrorResponse
            {
                Message = errorMessage ?? "Failed to define governance rule",
                ErrorCode = "RULE_DEFINITION_FAILED"
            });
        }

        // Fetch the created rule to return in response
        var rules = await _governanceService.GetGroupRulesAsync(groupId);
        var rule = rules.FirstOrDefault(r => r.Id == ruleId.Value);

        if (rule == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Message = "Rule created but could not be retrieved",
                ErrorCode = "RULE_RETRIEVAL_FAILED"
            });
        }

        _logger.LogInformation("Governance rule defined: {RuleId} for Group {GroupId}",
            ruleId.Value, groupId);

        var response = new GovernanceRuleResponse
        {
            Id = rule.Id,
            RuleType = rule.RuleType.ToString(),
            RuleValue = rule.RuleValue,
            Description = rule.Description,
            IsActive = rule.IsActive,
            ApprovedByVoteId = rule.ApprovedByVoteId,
            CreatedAt = rule.CreatedAt
        };

        return CreatedAtAction(
            nameof(GetConstitution),
            new { groupId },
            new ApiResponse<GovernanceRuleResponse>
            {
                Data = response,
                Message = "Governance rule defined successfully"
            });
    }

    /// <summary>
    /// Get all governance rules for a group (constitution)
    /// </summary>
    /// <remarks>
    /// Returns the complete constitution (all active governance rules) for a group.
    /// Includes rule type, value configuration, and approval details.
    /// </remarks>
    [HttpGet("constitution")]
    [ProducesResponseType(typeof(ApiResponse<ConstitutionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConstitution([FromRoute] Guid groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ErrorResponse
            {
                Message = "User not authenticated",
                ErrorCode = "AUTHENTICATION_REQUIRED"
            });
        }

        // Validate group exists
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
        {
            return NotFound(new ErrorResponse
            {
                Message = "Group not found",
                ErrorCode = "GROUP_NOT_FOUND"
            });
        }

        var rules = await _governanceService.GetGroupRulesAsync(groupId);

        var response = new ConstitutionResponse
        {
            GroupId = groupId,
            GroupName = group.Name,
            Rules = rules.Select(r => new GovernanceRuleResponse
            {
                Id = r.Id,
                RuleType = r.RuleType.ToString(),
                RuleValue = r.RuleValue,
                Description = r.Description,
                IsActive = r.IsActive,
                ApprovedByVoteId = r.ApprovedByVoteId,
                CreatedAt = r.CreatedAt
            }).ToList(),
            LastUpdated = rules.Any() ? rules.Max(r => r.CreatedAt) : DateTime.UtcNow
        };

        return Ok(new ApiResponse<ConstitutionResponse>
        {
            Data = response,
            Message = "Constitution retrieved successfully"
        });
    }

    /// <summary>
    /// Initiate a vote on a governance proposal
    /// </summary>
    /// <remarks>
    /// Allows group members to propose votes on rule changes, member removal, etc.
    /// Quorum threshold is read from governance rules (default 60%).
    /// Vote expires after specified duration (default 72 hours).
    /// </remarks>
    [HttpPost("votes")]
    [ProducesResponseType(typeof(ApiResponse<VoteResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitiateVote(
        [FromRoute] Guid groupId,
        [FromBody] InitiateVoteRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ErrorResponse
            {
                Message = "User not authenticated",
                ErrorCode = "AUTHENTICATION_REQUIRED"
            });
        }

        // Get member by ApplicationUserId
        var member = await _memberRepository.GetByApplicationUserIdAsync(userId);
        if (member == null)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Member profile not found",
                ErrorCode = "MEMBER_NOT_FOUND"
            });
        }

        // Validate JSON format for ProposalDetails
        try
        {
            JsonDocument.Parse(request.ProposalDetails);
        }
        catch
        {
            return BadRequest(new ErrorResponse
            {
                Message = "ProposalDetails must be valid JSON",
                ErrorCode = "INVALID_JSON_FORMAT"
            });
        }

        var (success, voteId, errorMessage) = await _governanceService.InitiateVoteAsync(
            groupId,
            member.Id,
            request.ProposalType,
            request.ProposalDetails,
            request.Summary ?? string.Empty,
            request.DurationHours);

        if (!success || !voteId.HasValue)
        {
            _logger.LogWarning("Vote initiation failed for Group {GroupId} by Member {MemberId}: {Error}",
                groupId, member.Id, errorMessage);

            return BadRequest(new ErrorResponse
            {
                Message = errorMessage ?? "Failed to initiate vote",
                ErrorCode = "VOTE_INITIATION_FAILED"
            });
        }

        // Fetch the created vote to return in response
        var votes = await _governanceService.GetActiveVotesAsync(groupId);
        var vote = votes.FirstOrDefault(v => v.Id == voteId.Value);

        if (vote == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Message = "Vote created but could not be retrieved",
                ErrorCode = "VOTE_RETRIEVAL_FAILED"
            });
        }

        _logger.LogInformation("Vote initiated: {VoteId} for Group {GroupId}",
            voteId.Value, groupId);

        var response = new VoteResponse
        {
            Id = vote.Id,
            GroupId = vote.GroupId,
            ProposalType = vote.ProposalType,
            ProposalDetails = vote.ProposalDetails,
            Summary = vote.Summary,
            VotesFor = vote.VotesFor,
            VotesAgainst = vote.VotesAgainst,
            VotesAbstain = vote.VotesAbstain,
            RequiredVotes = vote.RequiredVotes,
            Status = vote.Status.ToString(),
            CreatedAt = vote.CreatedAt,
            ExpiresAt = vote.ExpiresAt,
            HasVoted = false // Initiator hasn't voted yet
        };

        return CreatedAtAction(
            nameof(GetActiveVotes),
            new { groupId },
            new ApiResponse<VoteResponse>
            {
                Data = response,
                Message = "Vote initiated successfully. All members have been notified."
            });
    }

    /// <summary>
    /// Cast a vote on an active proposal
    /// </summary>
    /// <remarks>
    /// Allows group members to vote For, Against, or Abstain on active proposals.
    /// Each member can only vote once per proposal.
    /// Vote is auto-finalized when quorum is reached.
    /// </remarks>
    [HttpPost("votes/{voteId}/cast")]
    [ProducesResponseType(typeof(ApiResponse<VoteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CastVote(
        [FromRoute] Guid groupId,
        [FromRoute] Guid voteId,
        [FromBody] CastVoteRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ErrorResponse
            {
                Message = "User not authenticated",
                ErrorCode = "AUTHENTICATION_REQUIRED"
            });
        }

        // Get member by ApplicationUserId
        var member = await _memberRepository.GetByApplicationUserIdAsync(userId);
        if (member == null)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Member profile not found",
                ErrorCode = "MEMBER_NOT_FOUND"
            });
        }

        // Validate vote choice
        if (!new[] { "For", "Against", "Abstain" }.Contains(request.VoteChoice))
        {
            return BadRequest(new ErrorResponse
            {
                Message = "VoteChoice must be 'For', 'Against', or 'Abstain'",
                ErrorCode = "INVALID_VOTE_CHOICE"
            });
        }

        var (success, errorMessage) = await _governanceService.CastVoteAsync(
            voteId,
            member.Id,
            request.VoteChoice);

        if (!success)
        {
            _logger.LogWarning("Vote casting failed for Vote {VoteId} by Member {MemberId}: {Error}",
                voteId, member.Id, errorMessage);

            return BadRequest(new ErrorResponse
            {
                Message = errorMessage ?? "Failed to cast vote",
                ErrorCode = "VOTE_CASTING_FAILED"
            });
        }

        // Fetch the updated vote to return in response
        var votes = await _governanceService.GetActiveVotesAsync(groupId);
        var vote = votes.FirstOrDefault(v => v.Id == voteId);

        if (vote == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Message = "Vote cast but could not be retrieved",
                ErrorCode = "VOTE_RETRIEVAL_FAILED"
            });
        }

        _logger.LogInformation("Vote cast: {VoteId} by Member {MemberId} - {Choice}",
            voteId, member.Id, request.VoteChoice);

        var response = new VoteResponse
        {
            Id = vote.Id,
            GroupId = vote.GroupId,
            ProposalType = vote.ProposalType,
            ProposalDetails = vote.ProposalDetails,
            Summary = vote.Summary,
            VotesFor = vote.VotesFor,
            VotesAgainst = vote.VotesAgainst,
            VotesAbstain = vote.VotesAbstain,
            RequiredVotes = vote.RequiredVotes,
            Status = vote.Status.ToString(),
            CreatedAt = vote.CreatedAt,
            ExpiresAt = vote.ExpiresAt,
            HasVoted = true
        };

        return Ok(new ApiResponse<VoteResponse>
        {
            Data = response,
            Message = $"Vote recorded: {request.VoteChoice}"
        });
    }

    /// <summary>
    /// Get all active votes for a group
    /// </summary>
    /// <remarks>
    /// Returns all open votes that haven't expired yet.
    /// Indicates whether the current user has already voted.
    /// </remarks>
    [HttpGet("votes")]
    [ProducesResponseType(typeof(ApiResponse<List<VoteResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveVotes([FromRoute] Guid groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ErrorResponse
            {
                Message = "User not authenticated",
                ErrorCode = "AUTHENTICATION_REQUIRED"
            });
        }

        // Get member by ApplicationUserId
        var member = await _memberRepository.GetByApplicationUserIdAsync(userId);
        if (member == null)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Member profile not found",
                ErrorCode = "MEMBER_NOT_FOUND"
            });
        }

        var votes = await _governanceService.GetActiveVotesAsync(groupId);

        var response = votes.Select(v =>
        {
            // Check if current member has voted
            var voteRecords = JsonDocument.Parse(v.VoteRecords ?? "[]");
            var hasVoted = voteRecords.RootElement.EnumerateArray()
                .Any(record => record.GetProperty("memberId").GetGuid() == member.Id);

            return new VoteResponse
            {
                Id = v.Id,
                GroupId = v.GroupId,
                ProposalType = v.ProposalType,
                ProposalDetails = v.ProposalDetails,
                Summary = v.Summary,
                VotesFor = v.VotesFor,
                VotesAgainst = v.VotesAgainst,
                VotesAbstain = v.VotesAbstain,
                RequiredVotes = v.RequiredVotes,
                Status = v.Status.ToString(),
                CreatedAt = v.CreatedAt,
                ExpiresAt = v.ExpiresAt,
                HasVoted = hasVoted
            };
        }).ToList();

        return Ok(new ApiResponse<List<VoteResponse>>
        {
            Data = response,
            Message = $"Retrieved {response.Count} active vote(s)"
        });
    }

    /// <summary>
    /// Raise a dispute in the group
    /// </summary>
    /// <remarks>
    /// Allows group members to flag disputes for Chairperson review.
    /// Categories: MissedPayment, UnauthorizedWithdrawal, RuleViolation, etc.
    /// Chairperson is notified via SMS upon dispute creation.
    /// </remarks>
    [HttpPost("disputes")]
    [ProducesResponseType(typeof(ApiResponse<DisputeResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RaiseDispute(
        [FromRoute] Guid groupId,
        [FromBody] RaiseDisputeRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ErrorResponse
            {
                Message = "User not authenticated",
                ErrorCode = "AUTHENTICATION_REQUIRED"
            });
        }

        // Get member by ApplicationUserId
        var member = await _memberRepository.GetByApplicationUserIdAsync(userId);
        if (member == null)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Member profile not found",
                ErrorCode = "MEMBER_NOT_FOUND"
            });
        }

        var (success, disputeId, errorMessage) = await _governanceService.RaiseDisputeAsync(
            groupId,
            member.Id,
            request.Category,
            request.Description);

        if (!success || !disputeId.HasValue)
        {
            _logger.LogWarning("Dispute creation failed for Group {GroupId} by Member {MemberId}: {Error}",
                groupId, member.Id, errorMessage);

            return BadRequest(new ErrorResponse
            {
                Message = errorMessage ?? "Failed to raise dispute",
                ErrorCode = "DISPUTE_CREATION_FAILED"
            });
        }

        // Fetch the created dispute to return in response
        var disputes = await _governanceService.GetOpenDisputesAsync(groupId);
        var dispute = disputes.FirstOrDefault(d => d.Id == disputeId.Value);

        if (dispute == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse
            {
                Message = "Dispute created but could not be retrieved",
                ErrorCode = "DISPUTE_RETRIEVAL_FAILED"
            });
        }

        _logger.LogInformation("Dispute raised: {DisputeId} for Group {GroupId} by Member {MemberId}",
            disputeId.Value, groupId, member.Id);

        var response = new DisputeResponse
        {
            Id = dispute.Id,
            GroupId = dispute.GroupId,
            Category = dispute.Category,
            Description = dispute.Description,
            Status = dispute.Status.ToString(),
            Resolution = dispute.Resolution,
            CreatedAt = dispute.CreatedAt,
            ResolvedAt = dispute.ResolvedAt,
            EscalatedAt = dispute.EscalatedAt
        };

        return CreatedAtAction(
            nameof(GetOpenDisputes),
            new { groupId },
            new ApiResponse<DisputeResponse>
            {
                Data = response,
                Message = "Dispute raised successfully. Chairperson has been notified."
            });
    }

    /// <summary>
    /// Get all open disputes for a group
    /// </summary>
    /// <remarks>
    /// Returns disputes with status Open or ChairpersonReviewed.
    /// Visible to all group members for transparency.
    /// </remarks>
    [HttpGet("disputes")]
    [ProducesResponseType(typeof(ApiResponse<List<DisputeResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpenDisputes([FromRoute] Guid groupId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new ErrorResponse
            {
                Message = "User not authenticated",
                ErrorCode = "AUTHENTICATION_REQUIRED"
            });
        }

        var disputes = await _governanceService.GetOpenDisputesAsync(groupId);

        var response = disputes.Select(d => new DisputeResponse
        {
            Id = d.Id,
            GroupId = d.GroupId,
            Category = d.Category,
            Description = d.Description,
            Status = d.Status.ToString(),
            Resolution = d.Resolution,
            CreatedAt = d.CreatedAt,
            ResolvedAt = d.ResolvedAt,
            EscalatedAt = d.EscalatedAt
        }).ToList();

        return Ok(new ApiResponse<List<DisputeResponse>>
        {
            Data = response,
            Message = $"Retrieved {response.Count} open dispute(s)"
        });
    }
}
