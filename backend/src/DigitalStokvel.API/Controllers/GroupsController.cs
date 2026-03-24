using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DigitalStokvel.API.DTOs;
using DigitalStokvel.Services;
using DigitalStokvel.Core.Interfaces;

namespace DigitalStokvel.API.Controllers;

[ApiController]
[Route("api/v1/groups")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly GroupService _groupService;
    private readonly IMemberRepository _memberRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(
        GroupService groupService,
        IMemberRepository memberRepository,
        IGroupRepository groupRepository,
        ILogger<GroupsController> logger)
    {
        _groupService = groupService ?? throw new ArgumentNullException(nameof(groupService));
        _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Create a new stokvel group
    /// </summary>
    /// <remarks>
    /// Creates a new stokvel group with the authenticated user as Chairperson.
    /// Validates contribution amount (R50-R100,000) and FICA verification.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateGroupResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
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
                Message = "Member profile not found. Please complete your profile first.",
                ErrorCode = "MEMBER_NOT_FOUND"
            });
        }

        // Create the group
        var (success, group, errorMessage) = await _groupService.CreateGroupAsync(
            member.Id,
            request.Name,
            request.Description,
            request.GroupType,
            request.ContributionAmount,
            request.ContributionFrequency,
            request.ConstitutionRules);

        if (!success || group == null)
        {
            _logger.LogWarning("Group creation failed for Member {MemberId}: {Error}", 
                member.Id, errorMessage);

            var statusCode = errorMessage?.Contains("FICA") == true 
                ? StatusCodes.Status403Forbidden 
                : StatusCodes.Status400BadRequest;

            return StatusCode(statusCode, new ErrorResponse
            {
                Message = errorMessage ?? "We couldn't create your group this time. Let's try again!",
                ErrorCode = "GROUP_CREATION_FAILED"
            });
        }

        _logger.LogInformation("Group created successfully: {GroupId} by Member {MemberId}", 
            group.Id, member.Id);

        return CreatedAtAction(
            nameof(GetGroup),
            new { id = group.Id },
            new ApiResponse<CreateGroupResponse>
            {
                Message = "Your stokvel group is ready! You're the Chairperson.",
                Data = new CreateGroupResponse
                {
                    GroupId = group.Id,
                    GroupName = group.Name,
                    Role = "Chairperson",
                    GroupSavingsAccountNumber = group.GroupSavingsAccountNumber ?? "",
                    CreatedAt = group.CreatedAt
                }
            });
    }

    /// <summary>
    /// Get group details with roster
    /// </summary>
    /// <remarks>
    /// Returns detailed information about a group including member roster.
    /// Only accessible to members of the group.
    /// </remarks>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<GroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroup(Guid id)
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

        // Get member
        var member = await _memberRepository.GetByApplicationUserIdAsync(userId);
        if (member == null)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Member profile not found",
                ErrorCode = "MEMBER_NOT_FOUND"
            });
        }

        // Get group with members
        var group = await _groupRepository.GetGroupWithMembersAsync(id);
        if (group == null || !group.IsActive)
        {
            return NotFound(new ErrorResponse
            {
                Message = "Group not found",
                ErrorCode = "GROUP_NOT_FOUND"
            });
        }

        // Verify member is part of the group
        var isMember = group.Members?.Any(gm => gm.MemberId == member.Id && gm.IsActive) ?? false;
        if (!isMember)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse
            {
                Message = "You don't have access to this group",
                ErrorCode = "ACCESS_DENIED"
            });
        }

        // Build member roster response
        var memberResponses = new List<GroupMemberResponse>();
        if (group.Members != null)
        {
            foreach (var groupMember in group.Members.Where(gm => gm.IsActive))
            {
                var memberEntity = await _memberRepository.GetByIdAsync(groupMember.MemberId);
                if (memberEntity != null)
                {
                    memberResponses.Add(new GroupMemberResponse
                    {
                        MemberId = memberEntity.Id,
                        PhoneNumber = memberEntity.PhoneNumber,
                        Role = groupMember.Role,
                        JoinedDate = groupMember.JoinedDate,
                        IsActive = groupMember.IsActive
                    });
                }
            }
        }

        // Parse constitution
        Dictionary<string, object>? constitution = null;
        if (!string.IsNullOrWhiteSpace(group.Constitution))
        {
            try
            {
                constitution = JsonSerializer.Deserialize<Dictionary<string, object>>(group.Constitution);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse constitution for group {GroupId}", id);
            }
        }

        return Ok(new ApiResponse<GroupResponse>
        {
            Message = "Group details retrieved successfully",
            Data = new GroupResponse
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                GroupType = group.GroupType,
                ContributionAmount = group.ContributionAmount.Amount,
                ContributionFrequency = group.ContributionFrequency,
                Balance = group.Balance.Amount,
                GroupSavingsAccountNumber = group.GroupSavingsAccountNumber,
                MaxMembers = group.MaxMembers ?? 50, // Default to 50 if not set
                CurrentMemberCount = memberResponses.Count,
                IsActive = group.IsActive,
                CreatedAt = group.CreatedAt,
                Members = memberResponses,
                Constitution = constitution
            }
        });
    }

    /// <summary>
    /// Invite a member to join the group
    /// </summary>
    /// <remarks>
    /// Sends an SMS/push notification invite to the specified phone number.
    /// Only Chairperson and Secretary can invite members.
    /// </remarks>
    [HttpPut("{id}/members")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> InviteMember(Guid id, [FromBody] InviteMemberRequest request)
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

        // Get member
        var member = await _memberRepository.GetByApplicationUserIdAsync(userId);
        if (member == null)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Member profile not found",
                ErrorCode = "MEMBER_NOT_FOUND"
            });
        }

        // Invite member
        var (success, errorMessage) = await _groupService.InviteMemberAsync(
            id,
            member.Id,
            request.PhoneNumber);

        if (!success)
        {
            _logger.LogWarning("Member invitation failed for Group {GroupId}: {Error}", 
                id, errorMessage);

            var statusCode = errorMessage?.Contains("permission") == true || errorMessage?.Contains("Only") == true
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;

            return StatusCode(statusCode, new ErrorResponse
            {
                Message = errorMessage ?? "We couldn't send the invitation this time. Let's try again!",
                ErrorCode = "INVITATION_FAILED"
            });
        }

        _logger.LogInformation("Member invited to group {GroupId}: {PhoneNumber}", 
            id, request.PhoneNumber);

        return Ok(new ApiResponse<object>
        {
            Message = $"Invitation sent to {request.PhoneNumber}! They'll receive a notification shortly.",
            Data = new
            {
                PhoneNumber = request.PhoneNumber,
                GroupId = id,
                InvitedAt = DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// Assign a role to a group member
    /// </summary>
    /// <remarks>
    /// Assigns or updates a member's role in the group.
    /// Only the Chairperson can assign roles.
    /// Valid roles: Chairperson, Treasurer, Secretary, Member
    /// </remarks>
    [HttpPut("{id}/roles")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request)
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

        // Get member
        var member = await _memberRepository.GetByApplicationUserIdAsync(userId);
        if (member == null)
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Member profile not found",
                ErrorCode = "MEMBER_NOT_FOUND"
            });
        }

        // Assign role
        var (success, errorMessage) = await _groupService.AssignRoleAsync(
            id,
            member.Id,
            request.MemberId,
            request.Role);

        if (!success)
        {
            _logger.LogWarning("Role assignment failed for Group {GroupId}: {Error}", 
                id, errorMessage);

            var statusCode = errorMessage?.Contains("Chairperson") == true || errorMessage?.Contains("permission") == true
                ? StatusCodes.Status403Forbidden
                : StatusCodes.Status400BadRequest;

            return StatusCode(statusCode, new ErrorResponse
            {
                Message = errorMessage ?? "We couldn't assign the role this time. Let's try again!",
                ErrorCode = "ROLE_ASSIGNMENT_FAILED"
            });
        }

        _logger.LogInformation("Role assigned in group {GroupId}: Member {TargetMemberId} assigned {Role}", 
            id, request.MemberId, request.Role);

        return Ok(new ApiResponse<object>
        {
            Message = $"Role updated! The member is now a {request.Role}.",
            Data = new
            {
                GroupId = id,
                MemberId = request.MemberId,
                Role = request.Role,
                AssignedAt = DateTime.UtcNow
            }
        });
    }
}
