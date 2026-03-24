using DigitalStokvel.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DigitalStokvel.Services;

/// <summary>
/// Builds USSD menus with 3-level maximum depth validation (FR-036)
/// Structure: Main Menu → Category → Action (max 3 levels)
/// </summary>
public class UssdMenuBuilder
{
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<UssdMenuBuilder> _logger;
    private const int MaxMenuDepth = 3;

    public UssdMenuBuilder(
        ILocalizationService localizationService,
        ILogger<UssdMenuBuilder> logger)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Builds Main Menu (Level 1)
    /// 1=Pay Contribution, 2=Check Balance, 3=Transactions, 4=Language
    /// </summary>
    public string BuildMainMenu(string language)
    {
        var menu = _localizationService.GetString("ussd.main.title", language) + "\n";
        menu += "1. " + _localizationService.GetString("ussd.main.pay", language) + "\n";
        menu += "2. " + _localizationService.GetString("ussd.main.balance", language) + "\n";
        menu += "3. " + _localizationService.GetString("ussd.main.transactions", language) + "\n";
        menu += "4. " + _localizationService.GetString("ussd.main.language", language);

        return menu;
    }

    /// <summary>
    /// Builds Group Selection menu (Level 2)
    /// </summary>
    public string BuildGroupSelectionMenu(List<string> groupNames, string language)
    {
        if (groupNames.Count == 0)
        {
            return _localizationService.GetString("ussd.error.nogroups", language);
        }

        var menu = _localizationService.GetString("ussd.groups.title", language) + "\n";
        for (int i = 0; i < Math.Min(groupNames.Count, 9); i++)
        {
            menu += $"{i + 1}. {groupNames[i]}\n";
        }
        menu += "0. " + _localizationService.GetString("ussd.back", language);

        return menu;
    }

    /// <summary>
    /// Builds Confirmation menu (Level 3 - final level)
    /// </summary>
    public string BuildConfirmationMenu(string message, string language)
    {
        var menu = message + "\n";
        menu += "1. " + _localizationService.GetString("ussd.confirm", language) + "\n";
        menu += "2. " + _localizationService.GetString("ussd.cancel", language);

        return menu;
    }

    /// <summary>
    /// Builds Language Selection menu (Level 2)
    /// </summary>
    public string BuildLanguageMenu(string currentLanguage)
    {
        var menu = _localizationService.GetString("ussd.language.title", currentLanguage) + "\n";
        menu += "1. English\n";
        menu += "2. isiZulu\n";
        menu += "3. Sesotho\n";
        menu += "4. isiXhosa\n";
        menu += "5. Afrikaans\n";
        menu += "0. " + _localizationService.GetString("ussd.back", currentLanguage);

        return menu;
    }

    /// <summary>
    /// Builds Transaction History menu (Level 3)
    /// </summary>
    public string BuildTransactionHistoryMenu(List<string> transactions, string language, int page = 1)
    {
        if (transactions.Count == 0)
        {
            return _localizationService.GetString("ussd.error.notransactions", language);
        }

        var menu = _localizationService.GetString("ussd.transactions.title", language) + $" (Pg {page})\n";
        var startIndex = (page - 1) * 5;
        var endIndex = Math.Min(startIndex + 5, transactions.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            menu += transactions[i] + "\n";
        }

        if (endIndex < transactions.Count)
        {
            menu += "9. " + _localizationService.GetString("ussd.next", language) + "\n";
        }
        menu += "0. " + _localizationService.GetString("ussd.back", language);

        return menu;
    }

    /// <summary>
    /// Builds Balance Display (Level 3 - terminal screen)
    /// </summary>
    public string BuildBalanceDisplay(string groupName, decimal balance, decimal interest, string language)
    {
        var template = _localizationService.GetString("ussd.balance.display", language);
        return string.Format(template, groupName, balance.ToString("N2"), interest.ToString("N2"));
    }

    /// <summary>
    /// Builds Receipt Display (Level 3 - terminal screen)
    /// </summary>
    public string BuildReceiptDisplay(string groupName, decimal amount, string reference, string language)
    {
        var template = _localizationService.GetString("ussd.receipt.display", language);
        return string.Format(template, amount.ToString("N2"), groupName, reference);
    }

    /// <summary>
    /// Validates menu depth does not exceed 3 levels (FR-036)
    /// </summary>
    public (bool IsValid, string? ErrorMessage) ValidateMenuDepth(int currentDepth, string nextScreen)
    {
        var nextDepth = CalculateNextDepth(currentDepth, nextScreen);

        if (nextDepth > MaxMenuDepth)
        {
            _logger.LogWarning(
                "USSD menu depth exceeded | Current: {CurrentDepth} | Next: {NextDepth} | Screen: {NextScreen}",
                currentDepth, nextDepth, nextScreen);

            return (false, "Menu depth exceeds maximum allowed (3 levels)");
        }

        return (true, null);
    }

    private int CalculateNextDepth(int currentDepth, string nextScreen)
    {
        // Terminal screens don't increase depth
        if (nextScreen.Contains("Display") || nextScreen.Contains("Receipt") || nextScreen.Contains("Error"))
        {
            return currentDepth;
        }

        // Back navigation decreases depth
        if (nextScreen == "MainMenu" || nextScreen == "Back")
        {
            return Math.Max(0, currentDepth - 1);
        }

        // Forward navigation increases depth
        return currentDepth + 1;
    }
}
