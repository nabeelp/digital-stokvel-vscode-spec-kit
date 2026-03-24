using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DigitalStokvel.API.DTOs;
using DigitalStokvel.Core.Enums;
using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Services;

namespace DigitalStokvel.API.Controllers;

[ApiController]
[Route("api/v1/contributions")]
[Authorize]
public class ContributionsController : ControllerBase
{
    private readonly ContributionService _contributionService;
    private readonly ReceiptService _receiptService;
    private readonly IContributionRepository _contributionRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ILogger<ContributionsController> _logger;

    public ContributionsController(
        ContributionService contributionService,
        ReceiptService receiptService,
        IContributionRepository contributionRepository,
        IMemberRepository memberRepository,
        IGroupRepository groupRepository,
        IPaymentGateway paymentGateway,
        ILogger<ContributionsController> logger)
    {
        _contributionService = contributionService ?? throw new ArgumentNullException(nameof(contributionService));
        _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
        _contributionRepository = contributionRepository ?? throw new ArgumentNullException(nameof(contributionRepository));
        _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Make a contribution to a group
    /// </summary>
    /// <remarks>
    /// Processes a contribution payment with idempotency key to prevent duplicates.
    /// Supports OneTap, DebitOrder, and USSD payment methods.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ContributionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MakeContribution(
        [FromBody] MakeContributionRequest request,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey = null)
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

        // Validate idempotency key
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Idempotency key required in X-Idempotency-Key header",
                ErrorCode = "MISSING_IDEMPOTENCY_KEY"
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

        // Parse payment method
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
        {
            return BadRequest(new ErrorResponse
            {
                Message = "Invalid payment method. Must be OneTap, DebitOrder, or USSD",
                ErrorCode = "INVALID_PAYMENT_METHOD"
            });
        }

        // Process contribution
        var (success, contribution, errorMessage) = await _contributionService.ProcessContributionAsync(
            member.Id,
            request.GroupId,
            request.Amount,
            paymentMethod,
            idempotencyKey);

        if (!success || contribution == null)
        {
            _logger.LogWarning(
                "Contribution failed: Member {MemberId} | Group {GroupId} | Error: {Error}",
                member.Id, request.GroupId, errorMessage);

            return BadRequest(new ErrorResponse
            {
                Message = errorMessage ?? "Contribution processing failed",
                ErrorCode = "CONTRIBUTION_FAILED"
            });
        }

        // Get group details for response
        var group = await _groupRepository.GetByIdAsync(request.GroupId);

        // Generate receipt
        var receipt = _receiptService.GenerateReceipt(
            contribution,
            member.PhoneNumber,
            group?.Name ?? "Unknown Group",
            member.PreferredLanguage);

        _logger.LogInformation(
            "Contribution successful: {ContributionId} | Member {MemberId} | Amount: R{Amount}",
            contribution.Id, member.Id, request.Amount);

        return CreatedAtAction(
            nameof(GetContribution),
            new { id = contribution.Id },
            new ApiResponse<ContributionResponse>
            {
                Message = "Your contribution was successful! 🎉",
                Data = new ContributionResponse
                {
                    Id = contribution.Id,
                    GroupId = contribution.GroupId,
                    GroupName = group?.Name ?? "",
                    MemberId = member.Id,
                    MemberPhone = member.PhoneNumber,
                    Amount = contribution.Amount.Amount,
                    Currency = contribution.Amount.Currency,
                    PaymentMethod = contribution.PaymentMethod.ToString(),
                    Status = contribution.Status.ToString(),
                    Timestamp = contribution.Timestamp,
                    PaymentReference = contribution.PaymentGatewayReference,
                    Receipt = receipt
                }
            });
    }

    /// <summary>
    /// Get group contribution ledger
    /// </summary>
    /// <remarks>
    /// Returns group contribution history with POPIA-compliant data minimization.
    /// Account numbers are masked (****1234) to protect member privacy.
    /// </remarks>
    [HttpGet("ledger/{groupId}")]
    [ProducesResponseType(typeof(PagedResponse<LedgerEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetGroupLedger(
        Guid groupId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
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

        // Verify member is part of the group
        var group = await _groupRepository.GetGroupWithMembersAsync(groupId);
        if (group == null)
        {
            return NotFound(new ErrorResponse
            {
                Message = "Group not found",
                ErrorCode = "GROUP_NOT_FOUND"
            });
        }

        var isMember = group.Members?.Any(gm => gm.MemberId == member.Id && gm.IsActive) ?? false;
        if (!isMember)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse
            {
                Message = "You don't have access to this group's ledger",
                ErrorCode = "ACCESS_DENIED"
            });
        }

        // Get ledger
        var (contributions, totalCount) = await _contributionRepository.GetGroupLedgerAsync(groupId, page, pageSize);

        // Build ledger entries with POPIA compliance (masked account numbers)
        var ledgerEntries = contributions.Select(c => new LedgerEntryResponse
        {
            ContributionId = c.Id,
            MemberName = MaskMemberName(c.Member?.PhoneNumber ?? "Unknown"),
            MaskedAccountNumber = MaskAccountNumber(c.Member?.BankCustomerId),
            Amount = c.Amount.Amount,
            Status = c.Status.ToString(),
            Timestamp = c.Timestamp
        }).ToList();

        return Ok(new PagedResponse<LedgerEntryResponse>
        {
            Message = "Group ledger retrieved successfully",
            Data = ledgerEntries,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Get member's personal contribution history
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResponse<ContributionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMemberHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
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

        // Get contribution history
        var (contributions, totalCount) = await _contributionRepository.GetMemberHistoryAsync(member.Id, page, pageSize);

        // Build response
        var contributionResponses = contributions.Select(c => new ContributionResponse
        {
            Id = c.Id,
            GroupId = c.GroupId,
            GroupName = c.Group?.Name ?? "",
            MemberId = member.Id,
            MemberPhone = member.PhoneNumber,
            Amount = c.Amount.Amount,
            Currency = c.Amount.Currency,
            PaymentMethod = c.PaymentMethod.ToString(),
            Status = c.Status.ToString(),
            Timestamp = c.Timestamp,
            PaymentReference = c.PaymentGatewayReference
        }).ToList();

        return Ok(new PagedResponse<ContributionResponse>
        {
            Message = "Contribution history retrieved successfully",
            Data = contributionResponses,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    /// <summary>
    /// Setup recurring debit order for group contributions
    /// </summary>
    [HttpPost("debit-order")]
    [ProducesResponseType(typeof(ApiResponse<DebitOrderResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetupDebitOrder([FromBody] SetupDebitOrderRequest request)
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

        // Verify group exists and member is part of it
        var group = await _groupRepository.GetGroupWithMembersAsync(request.GroupId);
        if (group == null)
        {
            return NotFound(new ErrorResponse
            {
                Message = "Group not found",
                ErrorCode = "GROUP_NOT_FOUND"
            });
        }

        var isMember = group.Members?.Any(gm => gm.MemberId == member.Id && gm.IsActive) ?? false;
        if (!isMember)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse
            {
                Message = "You must be a member of this group to setup debit orders",
                ErrorCode = "ACCESS_DENIED"
            });
        }

        // Setup debit order via payment gateway
        var debitOrderResult = await _paymentGateway.SetupDebitOrderAsync(
            member.Id,
            request.Amount,
            request.Frequency,
            request.StartDate);

        if (!debitOrderResult.Success)
        {
            _logger.LogWarning(
                "Debit order setup failed: Member {MemberId} | Group {GroupId} | Error: {Error}",
                member.Id, request.GroupId, debitOrderResult.ErrorMessage);

            return BadRequest(new ErrorResponse
            {
                Message = debitOrderResult.ErrorMessage ?? "Failed to setup debit order",
                ErrorCode = "DEBIT_ORDER_SETUP_FAILED"
            });
        }

        _logger.LogInformation(
            "Debit order setup successful: Member {MemberId} | Group {GroupId} | DORef: {DebitOrderRef}",
            member.Id, request.GroupId, debitOrderResult.DebitOrderReference);

        return CreatedAtAction(
            nameof(SetupDebitOrder),
            new ApiResponse<DebitOrderResponse>
            {
                Message = "Debit order setup successful! Your contributions will be automatic.",
                Data = new DebitOrderResponse
                {
                    DebitOrderReference = debitOrderResult.DebitOrderReference ?? "",
                    GroupId = request.GroupId,
                    GroupName = group.Name,
                    Amount = request.Amount,
                    Frequency = request.Frequency,
                    NextDebitDate = debitOrderResult.NextDebitDate ?? DateTime.UtcNow
                }
            });
    }

    /// <summary>
    /// Get a specific contribution by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ContributionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContribution(Guid id)
    {
        var contribution = await _contributionRepository.GetByIdAsync(id);
        if (contribution == null)
        {
            return NotFound(new ErrorResponse
            {
                Message = "Contribution not found",
                ErrorCode = "CONTRIBUTION_NOT_FOUND"
            });
        }

        // Verify user has access (is the contributor)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var member = await _memberRepository.GetByApplicationUserIdAsync(userId ?? "");
        
        if (member == null || contribution.MemberId != member.Id)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse
            {
                Message = "Access denied",
                ErrorCode = "ACCESS_DENIED"
            });
        }

        var group = await _groupRepository.GetByIdAsync(contribution.GroupId);

        return Ok(new ApiResponse<ContributionResponse>
        {
            Data = new ContributionResponse
            {
                Id = contribution.Id,
                GroupId = contribution.GroupId,
                GroupName = group?.Name ?? "",
                MemberId = member.Id,
                MemberPhone = member.PhoneNumber,
                Amount = contribution.Amount.Amount,
                Currency = contribution.Amount.Currency,
                PaymentMethod = contribution.PaymentMethod.ToString(),
                Status = contribution.Status.ToString(),
                Timestamp = contribution.Timestamp,
                PaymentReference = contribution.PaymentGatewayReference
            }
        });
    }

    #region Private Helper Methods

    /// <summary>
    /// Masks member name for POPIA compliance (first name + last initial)
    /// </summary>
    private string MaskMemberName(string phoneNumber)
    {
        // In production, would retrieve actual member name and mask it
        // For now, show last 4 digits of phone number
        if (phoneNumber.Length >= 4)
        {
            return $"Member ****{phoneNumber[^4..]}";
        }
        return "Member ****";
    }

    /// <summary>
    /// Masks account number for POPIA compliance (****1234)
    /// </summary>
    private string MaskAccountNumber(string? bankCustomerId)
    {
        if (string.IsNullOrWhiteSpace(bankCustomerId) || bankCustomerId.Length < 4)
        {
            return "****0000";
        }

        return $"****{bankCustomerId[^4..]}";
    }

    #endregion
}
