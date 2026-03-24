using System.Text;
using Microsoft.Extensions.Logging;
using DigitalStokvel.Core.Entities;

namespace DigitalStokvel.Services;

/// <summary>
/// Service for generating branded shareable contribution receipts
/// </summary>
public class ReceiptService
{
    private readonly ILogger<ReceiptService> _logger;

    public ReceiptService(ILogger<ReceiptService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a branded receipt for a contribution
    /// </summary>
    /// <param name="contribution">Contribution entity</param>
    /// <param name="memberName">Member display name</param>
    /// <param name="groupName">Group name</param>
    /// <param name="language">Language code for localization</param>
    /// <returns>Receipt text suitable for SMS, mobile app, or sharing</returns>
    public string GenerateReceipt(
        Contribution contribution,
        string memberName,
        string groupName,
        string language = "en")
    {
        if (contribution == null)
            throw new ArgumentNullException(nameof(contribution));

        var receipt = new StringBuilder();

        // Header with branding
        receipt.AppendLine("🌟 DIGITAL STOKVEL 🌟");
        receipt.AppendLine(new string('=', 30));
        receipt.AppendLine();

        // Receipt title (localized)
        var title = GetLocalizedTitle(language);
        receipt.AppendLine(title);
        receipt.AppendLine();

        // Transaction details
        var detailsLabel = GetLocalizedLabel("details", language);
        receipt.AppendLine(detailsLabel);
        receipt.AppendLine(new string('-', 30));
        
        receipt.AppendLine($"{GetLocalizedLabel("group", language)}: {groupName}");
        receipt.AppendLine($"{GetLocalizedLabel("member", language)}: {memberName}");
        receipt.AppendLine($"{GetLocalizedLabel("amount", language)}: R{contribution.Amount.Amount:N2}");
        receipt.AppendLine($"{GetLocalizedLabel("method", language)}: {GetPaymentMethodDisplay(contribution.PaymentMethod, language)}");
        receipt.AppendLine($"{GetLocalizedLabel("date", language)}: {contribution.Timestamp:dd MMM yyyy HH:mm}");
        receipt.AppendLine($"{GetLocalizedLabel("reference", language)}: {contribution.PaymentGatewayReference ?? contribution.Id.ToString("N")[..8].ToUpper()}");
        receipt.AppendLine();

        // Status
        var statusLabel = GetLocalizedLabel("status", language);
        var statusValue = GetStatusDisplay(contribution.Status, language);
        receipt.AppendLine($"{statusLabel}: {statusValue}");
        receipt.AppendLine();

        // Footer with encouraging message
        receipt.AppendLine(new string('=', 30));
        var footerMessage = GetLocalizedFooter(language);
        receipt.AppendLine(footerMessage);
        receipt.AppendLine();
        receipt.AppendLine("www.digitalstokvel.co.za");

        _logger.LogInformation(
            "Receipt generated: Contribution {ContributionId} | Language: {Language}",
            contribution.Id, language);

        return receipt.ToString();
    }

    /// <summary>
    /// Generates a shareable receipt format suitable for WhatsApp/SMS
    /// </summary>
    public string GenerateShareableReceipt(
        Contribution contribution,
        string memberName,
        string groupName,
        string language = "en")
    {
        var receipt = new StringBuilder();

        receipt.AppendLine("✅ CONTRIBUTION RECEIPT");
        receipt.AppendLine($"Group: {groupName}");
        receipt.AppendLine($"Amount: R{contribution.Amount.Amount:N2}");
        receipt.AppendLine($"Date: {contribution.Timestamp:dd MMM yyyy}");
        receipt.AppendLine($"Ref: {contribution.PaymentGatewayReference ?? contribution.Id.ToString("N")[..8].ToUpper()}");
        receipt.AppendLine();
        receipt.AppendLine("📱 Digital Stokvel");
        receipt.AppendLine("Building financial futures together 💰");

        return receipt.ToString();
    }

    #region Private Helper Methods

    /// <summary>
    /// Gets localized receipt title
    /// </summary>
    private string GetLocalizedTitle(string language)
    {
        return language.ToLower() switch
        {
            "zu" => "IRISIDI YOKUNIKELA",
            "st" => "SEPHETHO SA SEKOLOTO",
            "xh" => "IRISITHI YEGALELO",
            "af" => "BYDRAE KWITANSIE",
            _ => "CONTRIBUTION RECEIPT"
        };
    }

    /// <summary>
    /// Gets localized label
    /// </summary>
    private string GetLocalizedLabel(string key, string language)
    {
        var labels = new Dictionary<string, Dictionary<string, string>>
        {
            ["details"] = new() { ["en"] = "Transaction Details", ["zu"] = "Imininingwane Yentengo", ["st"] = "Lintlha tsa Kgwebisano", ["xh"] = "Iinkcukacha Zentengo", ["af"] = "Transaksie Besonderhede" },
            ["group"] = new() { ["en"] = "Group", ["zu"] = "Iqembu", ["st"] = "Sehlopha", ["xh"] = "Iqela", ["af"] = "Groep" },
            ["member"] = new() { ["en"] = "Member", ["zu"] = "Ilungu", ["st"] = "Setho", ["xh"] = "Ilungu", ["af"] = "Lid" },
            ["amount"] = new() { ["en"] = "Amount", ["zu"] = "Inani", ["st"] = "Palo", ["xh"] = "Isixa-mali", ["af"] = "Bedrag" },
            ["method"] = new() { ["en"] = "Payment Method", ["zu"] = "Indlela Yokukhokha", ["st"] = "Mokhoa oa ho lefa", ["xh"] = "Indlela Yentlawulo", ["af"] = "Betaalmetode" },
            ["date"] = new() { ["en"] = "Date", ["zu"] = "Usuku", ["st"] = "Letlha", ["xh"] = "Umhla", ["af"] = "Datum" },
            ["reference"] = new() { ["en"] = "Reference", ["zu"] = "Inkomba", ["st"] = "Reference", ["xh"] = "Isalathiso", ["af"] = "Verwysing" },
            ["status"] = new() { ["en"] = "Status", ["zu"] = "Isimo", ["st"] = "Boemo", ["xh"] = "Ubume", ["af"] = "Status" }
        };

        if (labels.TryGetValue(key, out var translations))
        {
            return translations.GetValueOrDefault(language.ToLower(), translations["en"]);
        }

        return key;
    }

    /// <summary>
    /// Gets payment method display text
    /// </summary>
    private string GetPaymentMethodDisplay(Core.Enums.PaymentMethod method, string language)
    {
        return method switch
        {
            Core.Enums.PaymentMethod.OneTap => language.ToLower() switch
            {
                "zu" => "Thepha Kanye",
                "st" => "Tobetsa Hang",
                "xh" => "Cofa Kanye",
                "af" => "Een-Tik",
                _ => "One-Tap"
            },
            Core.Enums.PaymentMethod.DebitOrder => language.ToLower() switch
            {
                "zu" => "Umyalelo Wokukhipha",
                "st" => "Taelo ea Debit",
                "xh" => "Umyalelo Wokuhlawula",
                "af" => "Debietorder",
                _ => "Debit Order"
            },
            Core.Enums.PaymentMethod.USSD => "USSD",
            _ => method.ToString()
        };
    }

    /// <summary>
    /// Gets status display text
    /// </summary>
    private string GetStatusDisplay(Core.Enums.ContributionStatus status, string language)
    {
        return status switch
        {
            Core.Enums.ContributionStatus.Completed => language.ToLower() switch
            {
                "zu" => "✅ Kuphumelele",
                "st" => "✅ E atlehile",
                "xh" => "✅ Iphumelele",
                "af" => "✅ Suksesvol",
                _ => "✅ Successful"
            },
            Core.Enums.ContributionStatus.Pending => language.ToLower() switch
            {
                "zu" => "⏳ Kulindile",
                "st" => "⏳ E emetse",
                "xh" => "⏳ Ilindile",
                "af" => "⏳ Hangend",
                _ => "⏳ Pending"
            },
            Core.Enums.ContributionStatus.Failed => language.ToLower() switch
            {
                "zu" => "❌ Kwehlulekile",
                "st" => "❌ E hlolehile",
                "xh" => "❌ Ayiphumelelanga",
                "af" => "❌ Gefaal",
                _ => "❌ Failed"
            },
            Core.Enums.ContributionStatus.Retrying => language.ToLower() switch
            {
                "zu" => "🔄 Kuzama futhi",
                "st" => "🔄 E leka hape",
                "xh" => "🔄 Iyazama kwakhona",
                "af" => "🔄 Probeer weer",
                _ => "🔄 Retrying"
            },
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Gets localized footer message
    /// </summary>
    private string GetLocalizedFooter(string language)
    {
        return language.ToLower() switch
        {
            "zu" => "Siyabonga ngokunikela kwakho! 💚",
            "st" => "Rea leboha bakeng sa sekoloto sa hao! 💚",
            "xh" => "Siyabulela ngegalelo lakho! 💚",
            "af" => "Dankie vir jou bydrae! 💚",
            _ => "Thank you for your contribution! 💚"
        };
    }

    #endregion
}
