using System.Text;
using Microsoft.EntityFrameworkCore;
using DigitalStokvel.Infrastructure.Data;
using DigitalStokvel.Core.Entities;

namespace DigitalStokvel.Services;

/// <summary>
/// Service for exporting group ledgers to PDF format
/// Used for annual AGM records and audit trail compliance
/// </summary>
public class LedgerExportService
{
    private readonly ApplicationDbContext _context;

    public LedgerExportService(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Generate PDF ledger export for a group's annual records
    /// Returns: (Success, PdfBytes, ErrorMessage)
    /// </summary>
    public async Task<(bool Success, byte[]? PdfBytes, string? ErrorMessage)> ExportGroupLedgerToPdfAsync(
        Guid groupId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Fetch group details
            var group = await _context.StokvelsGroups
                .Include(g => g.Members)
                .ThenInclude(gm => gm.Member)
                .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

            if (group == null)
            {
                return (false, null, "Group not found");
            }

            // Fetch contributions for the period
            var contributions = await _context.Contributions
                .Include(c => c.Member)
                .Where(c => c.GroupId == groupId &&
                           c.Timestamp >= startDate &&
                           c.Timestamp <= endDate)
                .OrderBy(c => c.Timestamp)
                .ToListAsync(cancellationToken);

            // Fetch payouts for the period
            var payouts = await _context.Payouts
                .Include(p => p.Recipients)
                .Where(p => p.GroupId == groupId &&
                           p.CreatedAt >= startDate &&
                           p.CreatedAt <= endDate)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync(cancellationToken);

            // Generate HTML content (will be converted to PDF in production)
            var htmlContent = GenerateHtmlLedger(group, contributions, payouts, startDate, endDate);

            // In production, use a PDF library like QuestPDF, iTextSharp, or SelectPdf
            // For MVP, return HTML as UTF-8 bytes (can be rendered as PDF by browser)
            var pdfBytes = Encoding.UTF8.GetBytes(htmlContent);

            return (true, pdfBytes, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Failed to generate ledger PDF: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate HTML content for ledger (to be converted to PDF)
    /// </summary>
    private string GenerateHtmlLedger(
        StokvelsGroup group,
        List<Contribution> contributions,
        List<Payout> payouts,
        DateTime startDate,
        DateTime endDate)
    {
        var sb = new StringBuilder();

        // HTML header with styling
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset='UTF-8'>");
        sb.AppendLine("<title>Group Ledger Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("  body { font-family: Arial, sans-serif; margin: 40px; }");
        sb.AppendLine("  h1 { color: #2c5aa0; border-bottom: 3px solid #2c5aa0; padding-bottom: 10px; }");
        sb.AppendLine("  h2 { color: #555; margin-top: 30px; }");
        sb.AppendLine("  table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
        sb.AppendLine("  th { background-color: #2c5aa0; color: white; padding: 12px; text-align: left; }");
        sb.AppendLine("  td { padding: 10px; border-bottom: 1px solid #ddd; }");
        sb.AppendLine("  tr:hover { background-color: #f5f5f5; }");
        sb.AppendLine("  .summary { background-color: #f0f8ff; padding: 15px; border-radius: 5px; margin: 20px 0; }");
        sb.AppendLine("  .footer { margin-top: 40px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Header
        sb.AppendLine($"<h1>{group.Name} - Annual Ledger Report</h1>");
        sb.AppendLine($"<p><strong>Period:</strong> {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}</p>");
        sb.AppendLine($"<p><strong>Group Type:</strong> {group.GroupType}</p>");
        sb.AppendLine($"<p><strong>Contribution Amount:</strong> {group.ContributionAmount.Currency} {group.ContributionAmount.Amount:N2}</p>");
        sb.AppendLine($"<p><strong>Active Members:</strong> {group.Members.Count(m => m.IsActive)}</p>");
        sb.AppendLine($"<p><strong>Current Balance:</strong> {group.Balance.Currency} {group.Balance.Amount:N2}</p>");

        // Summary
        var totalContributions = contributions.Sum(c => c.Amount.Amount);
        var totalPayouts = payouts.Sum(p => p.TotalAmount.Amount);
        var netChange = totalContributions - totalPayouts;

        sb.AppendLine("<div class='summary'>");
        sb.AppendLine("<h2>Financial Summary</h2>");
        sb.AppendLine($"<p><strong>Total Contributions:</strong> R {totalContributions:N2}</p>");
        sb.AppendLine($"<p><strong>Total Payouts:</strong> R {totalPayouts:N2}</p>");
        sb.AppendLine($"<p><strong>Net Change:</strong> R {netChange:N2}</p>");
        sb.AppendLine("</div>");

        // Contributions table
        sb.AppendLine("<h2>Contributions</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Date</th><th>Member</th><th>Amount</th><th>Method</th><th>Status</th></tr>");

        foreach (var contribution in contributions)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{contribution.Timestamp:yyyy-MM-dd HH:mm}</td>");
            sb.AppendLine($"<td>{contribution.Member?.PhoneNumber ?? "Unknown"}</td>");
            sb.AppendLine($"<td>{contribution.Amount.Currency} {contribution.Amount.Amount:N2}</td>");
            sb.AppendLine($"<td>{contribution.PaymentMethod}</td>");
            sb.AppendLine($"<td>{contribution.Status}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");

        // Payouts table
        sb.AppendLine("<h2>Payouts</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Date</th><th>Type</th><th>Recipients</th><th>Amount</th><th>Status</th></tr>");

        foreach (var payout in payouts)
        {
            var recipientCount = payout.Recipients?.Count ?? 0;
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{payout.CreatedAt:yyyy-MM-dd HH:mm}</td>");
            sb.AppendLine($"<td>{payout.PayoutType}</td>");
            sb.AppendLine($"<td>{recipientCount} member(s)</td>");
            sb.AppendLine($"<td>{payout.TotalAmount.Currency} {payout.TotalAmount.Amount:N2}</td>");
            sb.AppendLine($"<td>{payout.Status}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");

        // Footer
        sb.AppendLine("<div class='footer'>");
        sb.AppendLine($"<p>Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>");
        sb.AppendLine($"<p>Digital Stokvel Banking Platform - Compliant with FICA and POPIA regulations</p>");
        sb.AppendLine("<p>This document serves as an official record for Annual General Meeting (AGM) purposes.</p>");
        sb.AppendLine("</div>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    /// <summary>
    /// Get ledger data as structured JSON for API consumers
    /// </summary>
    public async Task<(bool Success, object? Data, string? ErrorMessage)> GetLedgerDataAsync(
        Guid groupId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var group = await _context.StokvelsGroups
                .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

            if (group == null)
            {
                return (false, null, "Group not found");
            }

            var contributions = await _context.Contributions
                .Where(c => c.GroupId == groupId &&
                           c.Timestamp >= startDate &&
                           c.Timestamp <= endDate)
                .Select(c => new
                {
                    c.Id,
                    c.Timestamp,
                    MemberPhone = c.Member!.PhoneNumber,
                    Amount = c.Amount.Amount,
                    Currency = c.Amount.Currency,
                    c.PaymentMethod,
                    c.Status
                })
                .ToListAsync(cancellationToken);

            var payouts = await _context.Payouts
                .Where(p => p.GroupId == groupId &&
                           p.CreatedAt >= startDate &&
                           p.CreatedAt <= endDate)
                .Select(p => new
                {
                    p.Id,
                    CreatedAt = p.CreatedAt,
                    p.PayoutType,
                    RecipientCount = p.Recipients!.Count,
                    TotalAmount = p.TotalAmount.Amount,
                    Currency = p.TotalAmount.Currency,
                    p.Status
                })
                .ToListAsync(cancellationToken);

            var data = new
            {
                Group = new
                {
                    group.Id,
                    group.Name,
                    group.GroupType,
                    ContributionAmount = group.ContributionAmount.Amount,
                    Balance = group.Balance.Amount,
                    Currency = group.Balance.Currency
                },
                Period = new { startDate, endDate },
                Summary = new
                {
                    TotalContributions = contributions.Sum(c => c.Amount),
                    TotalPayouts = payouts.Sum(p => p.TotalAmount),
                    NetChange = contributions.Sum(c => c.Amount) - payouts.Sum(p => p.TotalAmount)
                },
                Contributions = contributions,
                Payouts = payouts
            };

            return (true, data, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Failed to retrieve ledger data: {ex.Message}");
        }
    }
}
