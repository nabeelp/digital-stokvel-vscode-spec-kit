using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Enums;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DigitalStokvel.Services;

/// <summary>
/// Service for managing governance, voting, and dispute resolution
/// </summary>
public class GovernanceService : IGovernanceService
{
    private readonly ApplicationDbContext _context;
    private readonly IGroupRepository _groupRepository;
    private readonly ISmsNotificationService _smsNotificationService;
    private readonly ILogger<GovernanceService> _logger;

    public GovernanceService(
        ApplicationDbContext context,
        IGroupRepository groupRepository,
        ISmsNotificationService smsNotificationService,
        ILogger<GovernanceService> logger)
    {
        _context = context;
        _groupRepository = groupRepository;
        _smsNotificationService = smsNotificationService;
        _logger = logger;
    }

    #region Constitution & Rules (T152)

    public async Task<(bool Success, Guid? RuleId, string? ErrorMessage)> DefineRuleAsync(
        Guid groupId,
        Guid memberId,
        RuleType ruleType,
        string ruleValue,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate member is Chairperson
            var isChairperson = await _groupRepository.HasRoleAsync(
                groupId, memberId, "Chairperson", cancellationToken);
                
            if (!isChairperson)
            {
                return (false, null, "Only Chairperson can define governance rules");
            }

            // Validate rule value is valid JSON
            try
            {
                JsonDocument.Parse(ruleValue);
            }
            catch
            {
                return (false, null, "Invalid rule value format. Must be valid JSON.");
            }

            // Check if rule already exists, update or create new
            var existingRule = await _context.GovernanceRules
                .FirstOrDefaultAsync(r => r.GroupId == groupId && r.RuleType == ruleType && r.IsActive, cancellationToken);

            if (existingRule != null)
            {
                // Deactivate old rule
                existingRule.IsActive = false;
                existingRule.ModifiedAt = DateTime.UtcNow;
                existingRule.ModifiedBy = memberId.ToString();
            }

            // Create new rule
            var rule = new GovernanceRule
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                RuleType = ruleType,
                RuleValue = ruleValue,
                Description = description ?? $"{ruleType} rule",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = memberId.ToString()
            };

            _context.GovernanceRules.Add(rule);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Governance rule defined: {RuleType} for group {GroupId} by {MemberId}",
                ruleType, groupId, memberId);

            return (true, rule.Id, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to define governance rule for group {GroupId}", groupId);
            return (false, null, $"Failed to define rule: {ex.Message}");
        }
    }

    public async Task<IEnumerable<GovernanceRule>> GetGroupRulesAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return await _context.GovernanceRules
            .Where(r => r.GroupId == groupId && r.IsActive)
            .OrderBy(r => r.RuleType)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Voting System (T155)

    public async Task<(bool Success, Guid? VoteId, string? ErrorMessage)> InitiateVoteAsync(
        Guid groupId,
        Guid initiatingMemberId,
        string proposalType,
        string proposalDetails,
        string summary,
        int durationHours = 72,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate member belongs to group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.MemberId == initiatingMemberId && gm.IsActive, cancellationToken);
                
            if (!isMember)
            {
                return (false, null, "Only group members can initiate votes");
            }

            // Get quorum threshold from group rules
            var quorumRule = await _context.GovernanceRules
                .FirstOrDefaultAsync(r => r.GroupId == groupId && 
                                        r.RuleType == RuleType.QuorumThreshold && 
                                        r.IsActive, cancellationToken);

            var quorumPercentage = 60; // Default 60%
            if (quorumRule != null)
            {
                try
                {
                    var ruleData = JsonDocument.Parse(quorumRule.RuleValue);
                    if (ruleData.RootElement.TryGetProperty("percentage", out var percentageElement))
                    {
                        quorumPercentage = percentageElement.GetInt32();
                    }
                }
                catch
                {
                    _logger.LogWarning("Failed to parse quorum rule for group {GroupId}, using default 60%", groupId);
                }
            }

            // Calculate required votes
            var memberCount = await _context.GroupMembers
                .CountAsync(gm => gm.GroupId == groupId && gm.IsActive, cancellationToken);
            var requiredVotes = (int)Math.Ceiling(memberCount * quorumPercentage / 100.0);

            // Create vote
            var vote = new QuorumVote
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                ProposalType = proposalType,
                ProposalDetails = proposalDetails,
                Summary = summary,
                InitiatedBy = initiatingMemberId,
                VotesFor = 0,
                VotesAgainst = 0,
                VotesAbstain = 0,
                RequiredVotes = requiredVotes,
                Status = "Open",
                ExpiresAt = DateTime.UtcNow.AddHours(durationHours),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = initiatingMemberId.ToString()
            };

            _context.QuorumVotes.Add(vote);
            await _context.SaveChangesAsync(cancellationToken);

            // Notify all group members
            await NotifyVoteInitiatedAsync(groupId, vote, cancellationToken);

            _logger.LogInformation(
                "Vote initiated: {VoteId} for group {GroupId} - {ProposalType}",
                vote.Id, groupId, proposalType);

            return (true, vote.Id, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate vote for group {GroupId}", groupId);
            return (false, null, $"Failed to initiate vote: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> CastVoteAsync(
        Guid voteId,
        Guid memberId,
        string vote,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var quorumVote = await _context.QuorumVotes
                .Include(v => v.Group)
                .FirstOrDefaultAsync(v => v.Id == voteId, cancellationToken);

            if (quorumVote == null)
            {
                return (false, "Vote not found");
            }

            if (quorumVote.Status != "Open")
            {
                return (false, $"Vote is {quorumVote.Status} and no longer accepting votes");
            }

            if (DateTime.UtcNow > quorumVote.ExpiresAt)
            {
                return (false, "Vote has expired");
            }

            // Validate member belongs to group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == quorumVote.GroupId && 
                              gm.MemberId == memberId && 
                              gm.IsActive, cancellationToken);
                              
            if (!isMember)
            {
                return (false, "Only group members can vote");
            }

            // Parse existing vote records
            var voteRecords = new List<Dictionary<string, string>>();
            try
            {
                if (!string.IsNullOrEmpty(quorumVote.VoteRecords) && quorumVote.VoteRecords != "[]")
                {
                    voteRecords = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(quorumVote.VoteRecords) 
                        ?? new List<Dictionary<string, string>>();
                }
            }
            catch
            {
                _logger.LogWarning("Failed to parse vote records for vote {VoteId}", voteId);
            }

            // Check if member already voted
            if (voteRecords.Any(r => r.TryGetValue("memberId", out var mid) && mid == memberId.ToString()))
            {
                return (false, "You have already cast your vote");
            }

            // Validate vote value
            if (vote != "For" && vote != "Against" && vote != "Abstain")
            {
                return (false, "Invalid vote. Must be 'For', 'Against', or 'Abstain'");
            }

            // Record vote
            voteRecords.Add(new Dictionary<string, string>
            {
                { "memberId", memberId.ToString() },
                { "vote", vote },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            });

            quorumVote.VoteRecords = JsonSerializer.Serialize(voteRecords);

            // Update vote counts
            switch (vote)
            {
                case "For":
                    quorumVote.VotesFor++;
                    break;
                case "Against":
                    quorumVote.VotesAgainst++;
                    break;
                case "Abstain":
                    quorumVote.VotesAbstain++;
                    break;
            }

            quorumVote.ModifiedAt = DateTime.UtcNow;
            quorumVote.ModifiedBy = memberId.ToString();

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Vote cast: {VoteId} by member {MemberId} - {Vote}",
                voteId, memberId, vote);

            // Check if quorum reached
            if (quorumVote.VotesFor >= quorumVote.RequiredVotes)
            {
                await ProcessVoteResultAsync(voteId, cancellationToken);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cast vote {VoteId}", voteId);
            return (false, $"Failed to cast vote: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Result, string? ErrorMessage)> ProcessVoteResultAsync(
        Guid voteId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var vote = await _context.QuorumVotes
                .Include(v => v.Group)
                .FirstOrDefaultAsync(v => v.Id == voteId, cancellationToken);

            if (vote == null)
            {
                return (false, string.Empty, "Vote not found");
            }

            if (vote.Status != "Open")
            {
                return (false, vote.Status, "Vote already processed");
            }

            // Determine result
            string result;
            if (vote.VotesFor >= vote.RequiredVotes)
            {
                result = "Approved";
            }
            else if (DateTime.UtcNow > vote.ExpiresAt)
            {
                result = "Expired";
            }
            else
            {
                result = "Rejected";
            }

            vote.Status = result;
            vote.FinalizedAt = DateTime.UtcNow;
            vote.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Vote {VoteId} finalized with result: {Result} ({VotesFor}/{RequiredVotes})",
                voteId, result, vote.VotesFor, vote.RequiredVotes);

            return (true, result, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process vote result {VoteId}", voteId);
            return (false, string.Empty, $"Failed to process vote: {ex.Message}");
        }
    }

    public async Task<IEnumerable<QuorumVote>> GetActiveVotesAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return await _context.QuorumVotes
            .Include(v => v.InitiatedByMember)
            .Where(v => v.GroupId == groupId && v.Status == "Open" && v.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Dispute Management (T156-T157)

    public async Task<(bool Success, Guid? DisputeId, string? ErrorMessage)> RaiseDisputeAsync(
        Guid groupId,
        Guid memberId,
        string category,
        string description,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate member belongs to group
            var isMember = await _context.GroupMembers
                .AnyAsync(gm => gm.GroupId == groupId && gm.MemberId == memberId && gm.IsActive, cancellationToken);
                
            if (!isMember)
            {
                return (false, null, "Only group members can raise disputes");
            }

            // Create dispute
            var dispute = new Dispute
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                RaisedBy = memberId,
                Category = category,
                Description = description,
                Status = DisputeStatus.Open,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = memberId.ToString()
            };

            _context.Disputes.Add(dispute);
            await _context.SaveChangesAsync(cancellationToken);

            // Notify Chairperson
            await NotifyChairpersonOfDisputeAsync(groupId, dispute, cancellationToken);

            _logger.LogInformation(
                "Dispute raised: {DisputeId} for group {GroupId} by {MemberId} - {Category}",
                dispute.Id, groupId, memberId, category);

            return (true, dispute.Id, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to raise dispute for group {GroupId}", groupId);
            return (false, null, $"Failed to raise dispute: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> ResolveDisputeAsync(
        Guid disputeId,
        Guid chairpersonId,
        string resolution,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dispute = await _context.Disputes
                .Include(d => d.Group)
                .FirstOrDefaultAsync(d => d.Id == disputeId, cancellationToken);

            if (dispute == null)
            {
                return (false, "Dispute not found");
            }

            // Validate member is Chairperson
            var isChairperson = await _groupRepository.HasRoleAsync(
                dispute.GroupId, chairpersonId, "Chairperson", cancellationToken);
                
            if (!isChairperson)
            {
                return (false, "Only Chairperson can resolve disputes");
            }

            if (dispute.Status == DisputeStatus.Resolved || dispute.Status == DisputeStatus.EscalatedToBank)
            {
                return (false, $"Dispute already {dispute.Status}");
            }

            dispute.Status = DisputeStatus.Resolved;
            dispute.Resolution = resolution;
            dispute.ResolvedAt = DateTime.UtcNow;
            dispute.ResolvedBy = chairpersonId;
            dispute.ModifiedAt = DateTime.UtcNow;
            dispute.ModifiedBy = chairpersonId.ToString();

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Dispute {DisputeId} resolved by Chairperson {ChairpersonId}",
                disputeId, chairpersonId);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve dispute {DisputeId}", disputeId);
            return (false, $"Failed to resolve dispute: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> EscalateDisputeToBankAsync(
        Guid disputeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dispute = await _context.Disputes
                .Include(d => d.Group)
                .FirstOrDefaultAsync(d => d.Id == disputeId, cancellationToken);

            if (dispute == null)
            {
                return (false, "Dispute not found");
            }

            // Check if 7 days have passed
            var daysSinceCreation = (DateTime.UtcNow - dispute.CreatedAt).TotalDays;
            if (daysSinceCreation < 7)
            {
                return (false, $"Dispute can only be escalated after 7 days. {Math.Ceiling(7 - daysSinceCreation)} days remaining.");
            }

            if (dispute.Status == DisputeStatus.Resolved)
            {
                return (false, "Dispute already resolved");
            }

            if (dispute.Status == DisputeStatus.EscalatedToBank)
            {
                return (false, "Dispute already escalated to bank");
            }

            dispute.Status = DisputeStatus.EscalatedToBank;
            dispute.EscalatedAt = DateTime.UtcNow;
            dispute.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Notify bank mediation team (stub)
            _logger.LogWarning(
                "STUB: Dispute {DisputeId} escalated to bank mediation. Bank team should be notified.",
                disputeId);

            _logger.LogInformation(
                "Dispute {DisputeId} escalated to bank mediation after {Days} days",
                disputeId, (int)daysSinceCreation);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to escalate dispute {DisputeId}", disputeId);
            return (false, $"Failed to escalate dispute: {ex.Message}");
        }
    }

    public async Task<IEnumerable<Dispute>> GetOpenDisputesAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Disputes
            .Include(d => d.RaisedByMember)
            .Where(d => d.GroupId == groupId && 
                       (d.Status == DisputeStatus.Open || d.Status == DisputeStatus.ChairpersonReviewed))
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Private Helper Methods

    private async Task NotifyVoteInitiatedAsync(Guid groupId, QuorumVote vote, CancellationToken cancellationToken)
    {
        try
        {
            var members = await _context.GroupMembers
                .Include(gm => gm.Member)
                .Where(gm => gm.GroupId == groupId && gm.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var groupMember in members)
            {
                var phoneNumber = groupMember.Member.PhoneNumber;
                var language = groupMember.Member.PreferredLanguage ?? "en";

                // Stub: Send SMS notification
                _logger.LogInformation(
                    "STUB: Notify member {PhoneNumber} about new vote {VoteId}: {Summary}",
                    phoneNumber, vote.Id, vote.Summary);

                await Task.Delay(10, cancellationToken); // Simulate notification
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify members of vote {VoteId}", vote.Id);
        }
    }

    private async Task NotifyChairpersonOfDisputeAsync(Guid groupId, Dispute dispute, CancellationToken cancellationToken)
    {
        try
        {
            var chairperson = await _context.GroupMembers
                .Include(gm => gm.Member)
                .FirstOrDefaultAsync(gm => gm.GroupId == groupId && 
                                          gm.Role == "Chairperson" && 
                                          gm.IsActive, cancellationToken);

            if (chairperson != null)
            {
                var phoneNumber = chairperson.Member.PhoneNumber;
                var language = chairperson.Member.PreferredLanguage ?? "en";

                _logger.LogInformation(
                    "STUB: Notify Chairperson {PhoneNumber} of new dispute {DisputeId}: {Category}",
                    phoneNumber, dispute.Id, dispute.Category);

                await Task.Delay(10, cancellationToken); // Simulate notification
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify Chairperson of dispute {DisputeId}", dispute.Id);
        }
    }

    #endregion
}
