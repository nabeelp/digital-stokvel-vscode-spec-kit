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

    /// <summary>
    /// T187: Generates a shareable branded receipt for group meetings (AGM, special meetings)
    /// Suitable for sharing via WhatsApp, SMS, or mobile apps
    /// </summary>
    /// <param name="groupName">Name of the stokvel group</param>
    /// <param name="meetingType">Type of meeting (AGM, Special Meeting, Emergency Meeting)</param>
    /// <param name="meetingDate">Date and time of the meeting</param>
    /// <param name="attendees">List of attending member names</param>
    /// <param name="decisions">Key decisions made during the meeting</param>
    /// <param name="nextMeetingDate">Date of next scheduled meeting (optional)</param>
    /// <param name="language">Language code for localization</param>
    /// <returns>Shareable meeting receipt/summary</returns>
    public string GenerateMeetingReceipt(
        string groupName,
        string meetingType,
        DateTime meetingDate,
        List<string> attendees,
        List<string> decisions,
        DateTime? nextMeetingDate = null,
        string language = "en")
    {
        if (string.IsNullOrWhiteSpace(groupName))
            throw new ArgumentNullException(nameof(groupName));
        if (attendees == null || attendees.Count == 0)
            throw new ArgumentException("At least one attendee required", nameof(attendees));

        var receipt = new StringBuilder();

        // Header with branding
        receipt.AppendLine("🌟 DIGITAL STOKVEL 🌟");
        receipt.AppendLine(new string('=', 35));
        receipt.AppendLine();

        // Meeting title
        var meetingTitle = GetLocalizedMeetingTitle(meetingType, language);
        receipt.AppendLine($"📋 {meetingTitle}");
        receipt.AppendLine();

        // Group and date
        receipt.AppendLine($"{GetLocalizedLabel("group", language)}: {groupName}");
        receipt.AppendLine($"{GetLocalizedLabel("date", language)}: {meetingDate:dd MMM yyyy HH:mm}");
        receipt.AppendLine();

        // Attendance
        var attendanceLabel = GetLocalizedMeetingLabel("attendance", language);
        receipt.AppendLine($"{attendanceLabel} ({attendees.Count}):");
        receipt.AppendLine(new string('-', 35));
        foreach (var attendee in attendees)
        {
            receipt.AppendLine($"✓ {attendee}");
        }
        receipt.AppendLine();

        // Key decisions
        if (decisions != null && decisions.Count > 0)
        {
            var decisionsLabel = GetLocalizedMeetingLabel("decisions", language);
            receipt.AppendLine($"{decisionsLabel}:");
            receipt.AppendLine(new string('-', 35));
            for (int i = 0; i < decisions.Count; i++)
            {
                receipt.AppendLine($"{i + 1}. {decisions[i]}");
            }
            receipt.AppendLine();
        }

        // Next meeting
        if (nextMeetingDate.HasValue)
        {
            var nextMeetingLabel = GetLocalizedMeetingLabel("next_meeting", language);
            receipt.AppendLine($"{nextMeetingLabel}:");
            receipt.AppendLine($"📅 {nextMeetingDate.Value:dd MMM yyyy HH:mm}");
            receipt.AppendLine();
        }

        // Footer
        receipt.AppendLine(new string('=', 35));
        var meetingFooter = GetLocalizedMeetingFooter(language);
        receipt.AppendLine(meetingFooter);
        receipt.AppendLine();
        receipt.AppendLine($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC");
        receipt.AppendLine("www.digitalstokvel.co.za");

        _logger.LogInformation(
            "Meeting receipt generated: Group {GroupName} | Type {MeetingType} | Attendees {AttendeeCount}",
            groupName, meetingType, attendees.Count);

        return receipt.ToString();
    }

    /// <summary>
    /// T187: Generates a compact meeting summary suitable for SMS notifications
    /// </summary>
    public string GenerateCompactMeetingSummary(
        string groupName,
        string meetingType,
        DateTime meetingDate,
        int attendeeCount,
        string language = "en")
    {
        var summary = new StringBuilder();

        summary.AppendLine($"📋 {GetLocalizedMeetingTitle(meetingType, language)}");
        summary.AppendLine($"Group: {groupName}");
        summary.AppendLine($"Date: {meetingDate:dd MMM yyyy}");
        summary.AppendLine($"Attendance: {attendeeCount} members");
        summary.AppendLine();
        summary.AppendLine("📱 Digital Stokvel");
        summary.AppendLine(GetLocalizedMeetingFooter(language));

        return summary.ToString();
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

    /// <summary>
    /// T187: Gets localized meeting title
    /// </summary>
    private string GetLocalizedMeetingTitle(string meetingType, string language)
    {
        var key = meetingType.ToLower().Replace(" ", "_");
        
        return (key, language.ToLower()) switch
        {
            ("agm", "zu") => "UMHLANGANO WONYAKA",
            ("agm", "st") => "KOPANO EA SELEMO",
            ("agm", "xh") => "INTLANGANISO YONYAKA",
            ("agm", "af") => "JAARLIKSE ALGEMENE VERGADERING",
            ("agm", _) => "ANNUAL GENERAL MEETING",
            
            ("special_meeting", "zu") => "UMHLANGANO OKHETHEKILE",
            ("special_meeting", "st") => "KOPANO E KHETHEHILENG",
            ("special_meeting", "xh") => "INTLANGANISO EKHETHEKILEYO",
            ("special_meeting", "af") => "SPESIALE VERGADERING",
            ("special_meeting", _) => "SPECIAL MEETING",
            
            ("emergency_meeting", "zu") => "UMHLANGANO WEZIPHUTHUMAYO",
            ("emergency_meeting", "st") => "KOPANO EA TSHOHANYETSO",
            ("emergency_meeting", "xh") => "INTLANGANISO YANGXAMISEKO",
            ("emergency_meeting", "af") => "NOODVERGADERING",
            ("emergency_meeting", _) => "EMERGENCY MEETING",
            
            _ => meetingType.ToUpper()
        };
    }

    /// <summary>
    /// T187: Gets localized meeting labels
    /// </summary>
    private string GetLocalizedMeetingLabel(string key, string language)
    {
        var labels = new Dictionary<string, Dictionary<string, string>>
        {
            ["attendance"] = new() 
            { 
                ["en"] = "Attendance", 
                ["zu"] = "Ukunakwa", 
                ["st"] = "Ho Teng", 
                ["xh"] = "Ukubakho", 
                ["af"] = "Bywoning" 
            },
            ["decisions"] = new() 
            { 
                ["en"] = "Key Decisions", 
                ["zu"] = "Izinqumo Ezibalulekile", 
                ["st"] = "Liqeto tsa Bohlokoa", 
                ["xh"] = "Izigqibo Eziphambili", 
                ["af"] = "Sleutelbesluite" 
            },
            ["next_meeting"] = new() 
            { 
                ["en"] = "Next Meeting", 
                ["zu"] = "Umhlangano Olandelayo", 
                ["st"] = "Kopano e Latelang", 
                ["xh"] = "Intlanganiso Elandelayo", 
                ["af"] = "Volgende Vergadering" 
            }
        };

        if (labels.TryGetValue(key, out var translations))
        {
            return translations.GetValueOrDefault(language.ToLower(), translations["en"]);
        }

        return key;
    }

    /// <summary>
    /// T187: Gets localized meeting footer
    /// </summary>
    private string GetLocalizedMeetingFooter(string language)
    {
        return language.ToLower() switch
        {
            "zu" => "Sibumbano simandla kunoma yikuphi 💪",
            "st" => "Re matla ha re kopane 💪",
            "xh" => "Simandla xa simanyanisiwe 💪",
            "af" => "Ons is sterker saam 💪",
            _ => "Together we are stronger 💪"
        };
    }

    #endregion
}
