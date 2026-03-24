using DigitalStokvel.Core.Enums;
using DigitalStokvel.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalStokvel.API.Controllers;

/// <summary>
/// Controller for payout operations
/// </summary>
[ApiController]
[Route("api/v1/payouts")]
public class PayoutsController : ControllerBase
{
    private readonly PayoutService _payoutService;
    private readonly ILogger<PayoutsController> _logger;

    public PayoutsController(
        PayoutService payoutService,
        ILogger<PayoutsController> logger)
    {
        _payoutService = payoutService;
        _logger = logger;
    }

    /// <summary>
    /// Initiates a new payout (Chairperson only)
    /// </summary>
    /// <remarks>
    /// Creates a payout pending Treasurer approval.
    /// 
    /// **Payout Types:**
    /// - RotatingCycle: One member receives principal only, interest stays in group
    /// - YearEndPot: Full balance distributed proportionally to all members
    /// - PartialWithdrawal: Specific amount requiring 60% quorum approval
    /// 
    /// **Authorization:** Only Chairperson can initiate payouts
    /// </remarks>
    [HttpPost("initiate")]
    [ProducesResponseType(typeof(InitiatePayoutResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InitiatePayoutResponse>> InitiatePayout(
        [FromBody] InitiatePayoutRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Get initiating member ID from authenticated user context
            var initiatingMemberId = Guid.Parse(request.InitiatingMemberId);

            var (success, payoutId, errorMessage) = await _payoutService.InitiatePayoutAsync(
                request.GroupId,
                initiatingMemberId,
                request.PayoutType,
                request.RecipientMemberId,
                request.Amount,
                request.Reason,
                cancellationToken);

            if (!success)
            {
                if (errorMessage?.Contains("Only Chairperson") == true)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                    {
                        Title = "Forbidden",
                        Detail = errorMessage,
                        Status = StatusCodes.Status403Forbidden
                    });
                }

                return BadRequest(new ProblemDetails
                {
                    Title = "Payout initiation failed",
                    Detail = errorMessage,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _logger.LogInformation(
                "Payout initiated: {PayoutId} by member {MemberId}",
                payoutId, initiatingMemberId);

            return CreatedAtAction(
                nameof(GetPayout),
                new { payoutId = payoutId },
                new InitiatePayoutResponse
                {
                    PayoutId = payoutId!.Value,
                    Status = request.PayoutType == PayoutType.PartialWithdrawal
                        ? PayoutStatus.PendingQuorum.ToString()
                        : PayoutStatus.PendingTreasurerApproval.ToString(),
                    Message = "Payout initiated successfully"
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payout for group {GroupId}", request.GroupId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Server error",
                Detail = "An error occurred while initiating the payout",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Confirms and executes a payout (Treasurer only)
    /// </summary>
    /// <remarks>
    /// Treasurer confirms the payout and system executes EFT disbursements.
    /// 
    /// **Authorization:** Only Treasurer can confirm payouts
    /// </remarks>
    [HttpPost("{payoutId}/confirm")]
    [ProducesResponseType(typeof(ConfirmPayoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConfirmPayoutResponse>> ConfirmPayout(
        [FromRoute] Guid payoutId,
        [FromBody] ConfirmPayoutRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // TODO: Get treasurer member ID from authenticated user context
            var treasurerMemberId = Guid.Parse(request.TreasurerMemberId);

            var (success, errorMessage) = await _payoutService.ConfirmPayoutAsync(
                payoutId,
                treasurerMemberId,
                cancellationToken);

            if (!success)
            {
                if (errorMessage?.Contains("not found") == true)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Payout not found",
                        Detail = errorMessage,
                        Status = StatusCodes.Status404NotFound
                    });
                }

                if (errorMessage?.Contains("Only Treasurer") == true)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
                    {
                        Title = "Forbidden",
                        Detail = errorMessage,
                        Status = StatusCodes.Status403Forbidden
                    });
                }

                return BadRequest(new ProblemDetails
                {
                    Title = "Payout confirmation failed",
                    Detail = errorMessage,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _logger.LogInformation(
                "Payout confirmed: {PayoutId} by treasurer {MemberId}",
                payoutId, treasurerMemberId);

            return Ok(new ConfirmPayoutResponse
            {
                PayoutId = payoutId,
                Status = PayoutStatus.Completed.ToString(),
                Message = "Payout executed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payout {PayoutId}", payoutId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Server error",
                Detail = "An error occurred while confirming the payout",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets payout history for a group
    /// </summary>
    /// <remarks>
    /// Returns all payouts for a group, ordered by creation date (most recent first).
    /// </remarks>
    [HttpGet("groups/{groupId}/history")]
    [ProducesResponseType(typeof(IEnumerable<PayoutHistoryItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PayoutHistoryItem>>> GetGroupPayouts(
        [FromRoute] Guid groupId,
        CancellationToken cancellationToken)
    {
        try
        {
            var payouts = await _payoutService.GetGroupPayoutHistoryAsync(groupId, cancellationToken);

            var response = payouts.Select(p => new PayoutHistoryItem
            {
                PayoutId = p.Id,
                PayoutType = p.PayoutType.ToString(),
                TotalAmount = p.TotalAmount.Amount,
                Status = p.Status.ToString(),
                InitiatedBy = p.InitiatedBy,
                ConfirmedBy = p.ConfirmedBy,
                CreatedAt = p.CreatedAt,
                CompletedAt = p.CompletedAt,
                RecipientCount = p.Recipients.Count,
                Reason = p.Reason
            });

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payout history for group {GroupId}", groupId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Server error",
                Detail = "An error occurred while retrieving payout history",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets a specific payout by ID
    /// </summary>
    [HttpGet("{payoutId}")]
    [ProducesResponseType(typeof(PayoutDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PayoutDetails>> GetPayout(
        [FromRoute] Guid payoutId,
        CancellationToken cancellationToken)
    {
        // Stub implementation for CreatedAtAction reference
        // In production, would call _payoutRepository.GetPayoutByIdAsync
        return NotFound(new ProblemDetails
        {
            Title = "Not implemented",
            Detail = "Payout details endpoint not yet implemented",
            Status = StatusCodes.Status404NotFound
        });
    }
}

#region DTOs

/// <summary>
/// Request to initiate a payout
/// </summary>
public record InitiatePayoutRequest
{
    /// <summary>
    /// Group ID
    /// </summary>
    public required Guid GroupId { get; init; }

    /// <summary>
    /// Member ID of initiating user (Chairperson)
    /// TODO: Remove when auth context is available
    /// </summary>
    public required string InitiatingMemberId { get; init; }

    /// <summary>
    /// Type of payout
    /// </summary>
    public required PayoutType PayoutType { get; init; }

    /// <summary>
    /// Recipient member ID (for RotatingCycle or PartialWithdrawal)
    /// </summary>
    public Guid? RecipientMemberId { get; init; }

    /// <summary>
    /// Amount (for PartialWithdrawal only)
    /// </summary>
    public decimal? Amount { get; init; }

    /// <summary>
    /// Reason for payout
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// Response from payout initiation
/// </summary>
public record InitiatePayoutResponse
{
    public required Guid PayoutId { get; init; }
    public required string Status { get; init; }
    public required string Message { get; init; }
}

/// <summary>
/// Request to confirm a payout
/// </summary>
public record ConfirmPayoutRequest
{
    /// <summary>
    /// Member ID of confirming user (Treasurer)
    /// TODO: Remove when auth context is available
    /// </summary>
    public required string TreasurerMemberId { get; init; }
}

/// <summary>
/// Response from payout confirmation
/// </summary>
public record ConfirmPayoutResponse
{
    public required Guid PayoutId { get; init; }
    public required string Status { get; init; }
    public required string Message { get; init; }
}

/// <summary>
/// Payout history item
/// </summary>
public record PayoutHistoryItem
{
    public required Guid PayoutId { get; init; }
    public required string PayoutType { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string Status { get; init; }
    public required Guid InitiatedBy { get; init; }
    public Guid? ConfirmedBy { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public required int RecipientCount { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Detailed payout information
/// </summary>
public record PayoutDetails
{
    public required Guid PayoutId { get; init; }
    public required string PayoutType { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string Status { get; init; }
    // Additional fields...
}

#endregion
