using DigitalStokvel.Core.Enums;

namespace DigitalStokvel.API.DTOs;

/// <summary>
/// Request DTO for making a contribution
/// </summary>
public record MakeContributionRequest
{
    public Guid GroupId { get; init; }
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = "OneTap"; // OneTap, DebitOrder, USSD
}

/// <summary>
/// Response DTO for contribution details
/// </summary>
public record ContributionResponse
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public Guid MemberId { get; init; }
    public string MemberPhone { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "ZAR";
    public string PaymentMethod { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string? PaymentReference { get; init; }
    public string? Receipt { get; init; }
}

/// <summary>
/// Response DTO for group ledger entry (POPIA-compliant with masked account numbers)
/// </summary>
public record LedgerEntryResponse
{
    public Guid ContributionId { get; init; }
    public string MemberName { get; init; } = string.Empty; // First name + last initial only
    public string MaskedAccountNumber { get; init; } = "****0000"; // e.g., "****1234"
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Request DTO for setting up recurring debit order
/// </summary>
public record SetupDebitOrderRequest
{
    public Guid GroupId { get; init; }
    public decimal Amount { get; init; }
    public string Frequency { get; init; } = "Monthly"; // Weekly, Biweekly, Monthly
    public DateTime StartDate { get; init; }
}

/// <summary>
/// Response DTO for debit order setup
/// </summary>
public record DebitOrderResponse
{
    public string DebitOrderReference { get; init; } = string.Empty;
    public Guid GroupId { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Frequency { get; init; } = string.Empty;
    public DateTime NextDebitDate { get; init; }
}
