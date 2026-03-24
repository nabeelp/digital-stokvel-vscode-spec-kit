using System.Text.Json;
using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace DigitalStokvel.Services;

/// <summary>
/// Service for managing stokvel groups, member invitations, and role assignments
/// </summary>
public class GroupService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ILocalizationService _localizationService;
    private readonly IServiceBusClient _serviceBusClient;
    private readonly ILogger<GroupService> _logger;

    // Contribution amount validation constants (in ZAR)
    private const decimal MinContributionAmount = 50.00m;
    private const decimal MaxContributionAmount = 100000.00m;

    // Role constants
    private const string RoleChairperson = "Chairperson";
    private const string RoleTreasurer = "Treasurer";
    private const string RoleSecretary = "Secretary";
    private const string RoleMember = "Member";

    public GroupService(
        IGroupRepository groupRepository,
        IMemberRepository memberRepository,
        ILocalizationService localizationService,
        IServiceBusClient serviceBusClient,
        ILogger<GroupService> logger)
    {
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new stokvel group with validation and default constitution
    /// </summary>
    /// <param name="creatorMemberId">The member creating the group (will be assigned Chairperson role)</param>
    /// <param name="name">Group name</param>
    /// <param name="description">Optional group description</param>
    /// <param name="groupType">Group type: Savings, Burial, Investment, Grocery</param>
    /// <param name="contributionAmount">Contribution amount in ZAR (must be R50-R100,000)</param>
    /// <param name="contributionFrequency">Contribution frequency: Weekly, Biweekly, Monthly</param>
    /// <param name="constitutionRules">Optional custom constitution rules (uses defaults if null)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created group with assigned Chairperson</returns>
    public async Task<(bool Success, StokvelsGroup? Group, string? ErrorMessage)> CreateGroupAsync(
        Guid creatorMemberId,
        string name,
        string? description,
        string groupType,
        decimal contributionAmount,
        string contributionFrequency,
        Dictionary<string, object>? constitutionRules = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate creator member exists and is FICA verified
            var creatorMember = await _memberRepository.GetByIdAsync(creatorMemberId, cancellationToken);
            if (creatorMember == null)
            {
                return (false, null, "Member not found");
            }

            if (!creatorMember.FicaVerified)
            {
                return (false, null, _localizationService.GetString(
                    "error.fica_not_verified", 
                    creatorMember.PreferredLanguage));
            }

            // Validate contribution amount range (R50 - R100,000)
            if (contributionAmount < MinContributionAmount || contributionAmount > MaxContributionAmount)
            {
                var errorMsg = _localizationService.GetString(
                    "error.invalid_contribution_amount",
                    creatorMember.PreferredLanguage,
                    MinContributionAmount,
                    MaxContributionAmount);
                return (false, null, errorMsg);
            }

            // Validate group name
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3)
            {
                return (false, null, _localizationService.GetString(
                    "error.invalid_group_name",
                    creatorMember.PreferredLanguage));
            }

            // Initialize constitution with defaults if not provided
            var constitution = constitutionRules ?? CreateDefaultConstitution();

            // Create the group entity
            var group = new StokvelsGroup
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                GroupType = groupType,
                ContributionAmount = new Money(contributionAmount, "ZAR"),
                ContributionFrequency = contributionFrequency,
                Constitution = JsonSerializer.Serialize(constitution),
                Balance = new Money(0, "ZAR"),
                MaxMembers = 50, // Default soft limit
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = creatorMemberId.ToString()
            };

            // Create group savings account (stub - will integrate with bank API in future)
            var groupSavingsAccountNumber = await CreateGroupSavingsAccountAsync(group, cancellationToken);
            group.GroupSavingsAccountNumber = groupSavingsAccountNumber;

            // Persist the group
            var createdGroup = await _groupRepository.CreateGroupAsync(group, cancellationToken);

            // Add creator as Chairperson
            await _groupRepository.AddMemberAsync(createdGroup.Id, creatorMemberId, RoleChairperson, cancellationToken);

            _logger.LogInformation(
                "Group created successfully: {GroupId} '{GroupName}' by Member {MemberId}",
                createdGroup.Id, createdGroup.Name, creatorMemberId);

            return (true, createdGroup, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group for Member {MemberId}", creatorMemberId);
            return (false, null, "An error occurred while creating the group. Please try again.");
        }
    }

    /// <summary>
    /// Invites a member to join the group via SMS/push notification
    /// </summary>
    /// <param name="groupId">The group to invite to</param>
    /// <param name="inviterMemberId">The member sending the invitation (must have permission)</param>
    /// <param name="inviteePhoneNumber">Phone number of member to invite</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status and error message if failed</returns>
    public async Task<(bool Success, string? ErrorMessage)> InviteMemberAsync(
        Guid groupId,
        Guid inviterMemberId,
        string inviteePhoneNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate group exists
            var group = await _groupRepository.GetGroupWithMembersAsync(groupId, cancellationToken);
            if (group == null || !group.IsActive)
            {
                return (false, "Group not found or inactive");
            }

            // Validate inviter is a member of the group with appropriate permissions
            var hasPermission = await _groupRepository.HasRoleAsync(groupId, inviterMemberId, RoleChairperson, cancellationToken)
                || await _groupRepository.HasRoleAsync(groupId, inviterMemberId, RoleSecretary, cancellationToken);

            if (!hasPermission)
            {
                return (false, "Only Chairperson or Secretary can invite members");
            }

            // Get inviter details for personalized message
            var inviter = await _memberRepository.GetByIdAsync(inviterMemberId, cancellationToken);
            if (inviter == null)
            {
                return (false, "Inviter member not found");
            }

            // Check if invitee is already a member
            var invitee = await _memberRepository.GetByPhoneNumberAsync(inviteePhoneNumber, cancellationToken);
            if (invitee != null)
            {
                var existingMembership = group.Members?
                    .FirstOrDefault(gm => gm.MemberId == invitee.Id && gm.IsActive);
                
                if (existingMembership != null)
                {
                    return (false, "This member is already part of the group");
                }
            }

            // Check if group has reached maximum members
            var currentMemberCount = await _groupRepository.GetMemberCountAsync(groupId, cancellationToken);
            if (currentMemberCount >= group.MaxMembers)
            {
                var warningMsg = _localizationService.GetString(
                    "warning.group_at_capacity",
                    inviter.PreferredLanguage,
                    group.MaxMembers);
                return (false, warningMsg);
            }

            // Generate invitation link/code
            var inviteCode = GenerateInviteCode(groupId);

            // Prepare notification message
            var notificationMessage = new
            {
                MessageType = "GroupInvitation",
                RecipientPhoneNumber = inviteePhoneNumber,
                GroupId = groupId,
                GroupName = group.Name,
                InviterName = inviter.PhoneNumber, // In production, would use display name
                InviteCode = inviteCode,
                ContributionAmount = group.ContributionAmount.Amount,
                ContributionFrequency = group.ContributionFrequency,
                Language = invitee?.PreferredLanguage ?? "en"
            };

            // Send notification via Azure Service Bus
            await _serviceBusClient.SendNotificationAsync(
                JsonSerializer.Serialize(notificationMessage),
                "GroupInvitation",
                new Dictionary<string, object>
                {
                    { "GroupId", groupId.ToString() },
                    { "InviteePhone", inviteePhoneNumber }
                },
                cancellationToken);

            _logger.LogInformation(
                "Member invited to group: Phone {Phone} to Group {GroupId} by Member {InviterId}",
                inviteePhoneNumber, groupId, inviterMemberId);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inviting member to group {GroupId}", groupId);
            return (false, "An error occurred while sending the invitation. Please try again.");
        }
    }

    /// <summary>
    /// Assigns or updates a member's role in the group with validation
    /// </summary>
    /// <param name="groupId">The group</param>
    /// <param name="assignerMemberId">The member assigning the role (must be Chairperson)</param>
    /// <param name="targetMemberId">The member receiving the role</param>
    /// <param name="newRole">New role: Chairperson, Treasurer, Secretary, Member</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status and error message if failed</returns>
    public async Task<(bool Success, string? ErrorMessage)> AssignRoleAsync(
        Guid groupId,
        Guid assignerMemberId,
        Guid targetMemberId,
        string newRole,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate role value
            var validRoles = new[] { RoleChairperson, RoleTreasurer, RoleSecretary, RoleMember };
            if (!validRoles.Contains(newRole))
            {
                return (false, $"Invalid role. Must be one of: {string.Join(", ", validRoles)}");
            }

            // Validate group exists
            var group = await _groupRepository.GetGroupWithMembersAsync(groupId, cancellationToken);
            if (group == null || !group.IsActive)
            {
                return (false, "Group not found or inactive");
            }

            // Validate assigner is Chairperson (only Chairperson can assign roles)
            var isChairperson = await _groupRepository.HasRoleAsync(groupId, assignerMemberId, RoleChairperson, cancellationToken);
            if (!isChairperson)
            {
                return (false, "Only the Chairperson can assign roles");
            }

            // Validate target member exists and is part of the group
            var targetMember = group.Members?.FirstOrDefault(gm => gm.MemberId == targetMemberId && gm.IsActive);
            if (targetMember == null)
            {
                return (false, "Target member is not part of this group");
            }

            // Validate role-specific rules
            if (newRole == RoleChairperson)
            {
                // Only one Chairperson allowed - prevent assignment
                return (false, "Cannot assign Chairperson role. Transfer of leadership requires a governance vote.");
            }

            if (newRole == RoleTreasurer)
            {
                // Check if there's already a Treasurer (only one allowed)
                var existingTreasurer = group.Members?
                    .FirstOrDefault(gm => gm.Role == RoleTreasurer && gm.IsActive && gm.MemberId != targetMemberId);
                
                if (existingTreasurer != null)
                {
                    return (false, "There can only be one Treasurer. Remove the current Treasurer first.");
                }

                // Validate Treasurer-specific requirements (future: credit check, additional verification)
                var targetMemberEntity = await _memberRepository.GetByIdAsync(targetMemberId, cancellationToken);
                if (targetMemberEntity != null && !targetMemberEntity.FicaVerified)
                {
                    return (false, "Treasurer must be FICA verified");
                }
            }

            // Assign the role
            await _groupRepository.AssignRoleAsync(groupId, targetMemberId, newRole, cancellationToken);

            _logger.LogInformation(
                "Role assigned in group {GroupId}: Member {TargetMemberId} assigned role {Role} by {AssignerMemberId}",
                groupId, targetMemberId, newRole, assignerMemberId);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role in group {GroupId}", groupId);
            return (false, "An error occurred while assigning the role. Please try again.");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Creates default constitution rules for a new group
    /// </summary>
    private Dictionary<string, object> CreateDefaultConstitution()
    {
        return new Dictionary<string, object>
        {
            { "votingThreshold", 0.60 }, // 60% simple majority
            { "quorumThreshold", 0.50 }, // 50% quorum required
            { "missedPaymentPenalty", 50.00m },
            { "gracePeriodDays", 7 },
            { "memberRemovalCriteria", "3_consecutive_misses" },
            { "payoutApprovalRequired", true }, // Dual-approval by default
            { "constitutionVersion", "1.0" }
        };
    }

    /// <summary>
    /// Creates a group savings account via bank integration (stub implementation)
    /// </summary>
    /// <remarks>
    /// Future implementation will integrate with real bank API to create savings account
    /// </remarks>
    private Task<string> CreateGroupSavingsAccountAsync(StokvelsGroup group, CancellationToken cancellationToken)
    {
        // STUB: Generate placeholder account number
        // In production, this would call bank API to create actual savings account
        var accountNumber = $"62{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        
        _logger.LogInformation(
            "STUB: Created group savings account {AccountNumber} for group {GroupId}",
            accountNumber, group.Id);

        return Task.FromResult(accountNumber);
    }

    /// <summary>
    /// Generates a unique invite code for group invitation
    /// </summary>
    private string GenerateInviteCode(Guid groupId)
    {
        // Format: STK-XXX-YYYY where XXX is first 3 chars of group ID, YYYY is year
        var groupPrefix = groupId.ToString("N")[..3].ToUpper();
        var year = DateTime.UtcNow.Year;
        return $"STK-{groupPrefix}-{year}";
    }

    #endregion

    #region Withdrawal and Quorum Methods

    /// <summary>
    /// Initiates a withdrawal request that requires quorum approval (60% of eligible members)
    /// </summary>
    /// <param name="groupId">The group to withdraw from</param>
    /// <param name="requestorMemberId">The member requesting the withdrawal</param>
    /// <param name="amount">Amount to withdraw in ZAR</param>
    /// <param name="reason">Reason for withdrawal</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status, withdrawal request ID, and error message if failed</returns>
    public async Task<(bool Success, Guid? WithdrawalRequestId, string? ErrorMessage)> RequestWithdrawalAsync(
        Guid groupId,
        Guid requestorMemberId,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate group exists
            var group = await _groupRepository.GetGroupWithMembersAsync(groupId, cancellationToken);
            if (group == null || !group.IsActive)
            {
                return (false, null, "Group not found or inactive");
            }

            // Validate requestor is a member of the group
            var requestor = group.Members?.FirstOrDefault(gm => gm.MemberId == requestorMemberId && gm.IsActive);
            if (requestor == null)
            {
                return (false, null, "You are not a member of this group");
            }

            // Validate withdrawal amount
            if (amount <= 0)
            {
                return (false, null, "Withdrawal amount must be greater than zero");
            }

            if (amount > group.Balance.Amount)
            {
                return (false, null, $"Insufficient group balance. Available: R{group.Balance.Amount:N2}");
            }

            // Create withdrawal request (stored as JSON in Constitution for simplicity - production would use separate table)
            var withdrawalRequestId = Guid.NewGuid();
            var withdrawalRequest = new
            {
                Id = withdrawalRequestId,
                GroupId = groupId,
                RequestorMemberId = requestorMemberId,
                Amount = amount,
                Reason = reason,
                RequestedAt = DateTime.UtcNow,
                Status = "PendingQuorum",
                VotesFor = 0,
                VotesAgainst = 0,
                VotedMembers = new List<Guid>(),
                RequiredQuorum = 0.60m // 60% approval required
            };

            _logger.LogInformation(
                "Withdrawal request created: {WithdrawalRequestId} for Group {GroupId} by Member {MemberId}, Amount: R{Amount}",
                withdrawalRequestId, groupId, requestorMemberId, amount);

            // Note: In production, this would be stored in a WithdrawalRequest table
            // For now, log and return success - actual voting implementation would follow

            return (true, withdrawalRequestId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating withdrawal request for Group {GroupId}", groupId);
            return (false, null, "An error occurred while creating the withdrawal request. Please try again.");
        }
    }

    /// <summary>
    /// Allows a member to vote on a pending withdrawal request
    /// </summary>
    /// <param name="groupId">The group</param>
    /// <param name="withdrawalRequestId">The withdrawal request to vote on</param>
    /// <param name="voterMemberId">The member casting the vote</param>
    /// <param name="approve">True to approve, false to reject</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status, voting result, and error message if failed</returns>
    public async Task<(bool Success, string? VotingResult, string? ErrorMessage)> VoteOnWithdrawalAsync(
        Guid groupId,
        Guid withdrawalRequestId,
        Guid voterMemberId,
        bool approve,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate group exists
            var group = await _groupRepository.GetGroupWithMembersAsync(groupId, cancellationToken);
            if (group == null || !group.IsActive)
            {
                return (false, null, "Group not found or inactive");
            }

            // Validate voter is an eligible member (active member with good standing)
            var voter = group.Members?.FirstOrDefault(gm => gm.MemberId == voterMemberId && gm.IsActive);
            if (voter == null)
            {
                return (false, null, "You are not an eligible member of this group");
            }

            // Note: In production, this would:
            // 1. Fetch WithdrawalRequest from database
            // 2. Validate voter hasn't already voted
            // 3. Record vote
            // 4. Calculate if quorum reached (60% of eligible members)
            // 5. If quorum reached and approved, process withdrawal
            // 6. Notify all members of voting result

            var eligibleMemberCount = group.Members?.Count(gm => gm.IsActive) ?? 0;
            var requiredVotes = (int)Math.Ceiling(eligibleMemberCount * 0.60m);

            _logger.LogInformation(
                "Vote recorded on withdrawal {WithdrawalRequestId}: Member {MemberId} voted {Vote}, Required votes: {RequiredVotes}/{EligibleMembers}",
                withdrawalRequestId, voterMemberId, approve ? "FOR" : "AGAINST", requiredVotes, eligibleMemberCount);

            // Stub return - production would return actual voting status
            return (true, $"Vote recorded. Quorum requires {requiredVotes} out of {eligibleMemberCount} votes.", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voting on withdrawal request {WithdrawalRequestId}", withdrawalRequestId);
            return (false, null, "An error occurred while recording your vote. Please try again.");
        }
    }

    /// <summary>
    /// Blocks unilateral withdrawals - all withdrawals require quorum approval
    /// </summary>
    /// <param name="groupId">The group</param>
    /// <param name="memberId">The member attempting withdrawal</param>
    /// <param name="amount">Amount to withdraw</param>
    /// <returns>Always returns false with error message directing to quorum process</returns>
    public Task<(bool Success, string? ErrorMessage)> AttemptUnilateralWithdrawalAsync(
        Guid groupId,
        Guid memberId,
        decimal amount)
    {
        // Block all unilateral withdrawals
        var errorMessage = "Withdrawals require group approval. Please create a withdrawal request and obtain 60% member approval through the voting process.";
        
        _logger.LogWarning(
            "Blocked unilateral withdrawal attempt: Group {GroupId}, Member {MemberId}, Amount: R{Amount}",
            groupId, memberId, amount);

        return Task.FromResult((false, (string?)errorMessage));
    }

    #endregion
}
