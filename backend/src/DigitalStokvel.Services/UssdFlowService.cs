using DigitalStokvel.Core.Interfaces;
using DigitalStokvel.Infrastructure.USSD;
using Microsoft.Extensions.Logging;

namespace DigitalStokvel.Services;

/// <summary>
/// Implements USSD user flows for Pay Contribution, Check Balance, and View Transactions
/// </summary>
public class UssdFlowService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IContributionRepository _contributionRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly UssdMenuBuilder _menuBuilder;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<UssdFlowService> _logger;

    public UssdFlowService(
        IGroupRepository groupRepository,
        IContributionRepository contributionRepository,
        IPaymentGateway paymentGateway,
        UssdMenuBuilder menuBuilder,
        ILocalizationService localizationService,
        ILogger<UssdFlowService> logger)
    {
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        _contributionRepository = contributionRepository ?? throw new ArgumentNullException(nameof(contributionRepository));
        _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
        _menuBuilder = menuBuilder ?? throw new ArgumentNullException(nameof(menuBuilder));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Pay Contribution Flow: Group Selection → Amount Confirmation → PIN Auth → Receipt (T099)
    /// </summary>
    public async Task<(string ResponseText, string NextScreen, Dictionary<string, string> UpdatedContext)> ProcessPayContributionFlowAsync(
        UssdSessionState session,
        string userInput,
        CancellationToken cancellationToken = default)
    {
        var context = session.Context;
        var language = session.Language;

        // Step 1: Group Selection
        if (session.CurrentScreen == "PayContribution_SelectGroup")
        {
            var memberId = Guid.Parse(context.GetValueOrDefault("MemberId", Guid.Empty.ToString()));
            var groups = await _groupRepository.GetMemberGroupsAsync(memberId, cancellationToken);
            var groupList = groups.Select(g => g.Name).ToList();

            if (!int.TryParse(userInput, out int groupIndex) || groupIndex < 1 || groupIndex > groupList.Count)
            {
                return (_localizationService.GetString("ussd.error.invalid", language), session.CurrentScreen, context);
            }

            var selectedGroup = groups.ElementAt(groupIndex - 1);
            context["SelectedGroupId"] = selectedGroup.Id.ToString();
            context["SelectedGroupName"] = selectedGroup.Name;
            context["ContributionAmount"] = selectedGroup.ContributionAmount.Amount.ToString();

            var confirmMsg = string.Format(
                _localizationService.GetString("ussd.pay.confirm", language),
                selectedGroup.Name,
                selectedGroup.ContributionAmount.Amount.ToString("N2"));

            return (_menuBuilder.BuildConfirmationMenu(confirmMsg, language), "PayContribution_Confirm", context);
        }

        // Step 2: Amount Confirmation
        if (session.CurrentScreen == "PayContribution_Confirm")
        {
            if (userInput == "1") // Confirm
            {
                var pinPrompt = _localizationService.GetString("ussd.pay.enterpin", language);
                return (pinPrompt, "PayContribution_PIN", context);
            }
            else // Cancel
            {
                return (_localizationService.GetString("ussd.cancelled", language), "MainMenu", new Dictionary<string, string>());
            }
        }

        // Step 3: PIN Authentication (T105)
        if (session.CurrentScreen == "PayContribution_PIN")
        {
            var pin = userInput;
            var isValidPin = await ValidateBankPINAsync(context["MemberId"], pin, cancellationToken);

            if (!isValidPin)
            {
                return (_localizationService.GetString("ussd.pay.invalidpin", language), "MainMenu", new Dictionary<string, string>());
            }

            // Process payment
            var groupId = Guid.Parse(context["SelectedGroupId"]);
            var memberId = Guid.Parse(context["MemberId"]);
            var amount = decimal.Parse(context["ContributionAmount"]);

            var paymentResult = await _paymentGateway.DeductFromAccountAsync(
                memberId,
                amount,
                "ZAR",
                null,
                cancellationToken);

            if (paymentResult.Success)
            {
                var receipt = _menuBuilder.BuildReceiptDisplay(
                    context["SelectedGroupName"],
                    amount,
                    paymentResult.TransactionReference ?? "N/A",
                    language);

                return (receipt, "MainMenu", new Dictionary<string, string>());
            }
            else
            {
                return (_localizationService.GetString("ussd.pay.failed", language), "MainMenu", new Dictionary<string, string>());
            }
        }

        return ("Error", "MainMenu", context);
    }

    /// <summary>
    /// Check Balance Flow: Group Selection → Display Balance + Interest (T100)
    /// </summary>
    public async Task<(string ResponseText, string NextScreen)> ProcessCheckBalanceFlowAsync(
        UssdSessionState session,
        string userInput,
        CancellationToken cancellationToken = default)
    {
        var language = session.Language;

        if (session.CurrentScreen == "CheckBalance_SelectGroup")
        {
            var memberId = Guid.Parse(session.Context.GetValueOrDefault("MemberId", Guid.Empty.ToString()));
            var groups = await _groupRepository.GetMemberGroupsAsync(memberId, cancellationToken);

            if (!int.TryParse(userInput, out int groupIndex) || groupIndex < 1 || groupIndex > groups.Count())
            {
                return (_localizationService.GetString("ussd.error.invalid", language), session.CurrentScreen);
            }

            var selectedGroup = groups.ElementAt(groupIndex - 1);
            // TODO: Calculate actual accrued interest from contributions
            var accruedInterest = 0.00m; // Stub for MVP
            var balanceDisplay = _menuBuilder.BuildBalanceDisplay(
                selectedGroup.Name,
                selectedGroup.Balance.Amount,
                accruedInterest,
                language);

            return (balanceDisplay, "MainMenu");
        }

        return ("Error", "MainMenu");
    }

    /// <summary>
    /// View Transactions Flow: Group Selection → Last 5 transactions with pagination (T101)
    /// </summary>
    public async Task<(string ResponseText, string NextScreen, Dictionary<string, string> UpdatedContext)> ProcessViewTransactionsFlowAsync(
        UssdSessionState session,
        string userInput,
        CancellationToken cancellationToken = default)
    {
        var context = session.Context;
        var language = session.Language;

        // Step 1: Group Selection
        if (session.CurrentScreen == "ViewTransactions_SelectGroup")
        {
            var memberId = Guid.Parse(context.GetValueOrDefault("MemberId", Guid.Empty.ToString()));
            var groups = await _groupRepository.GetMemberGroupsAsync(memberId, cancellationToken);

            if (!int.TryParse(userInput, out int groupIndex) || groupIndex < 1 || groupIndex > groups.Count())
            {
                return (_localizationService.GetString("ussd.error.invalid", language), session.CurrentScreen, context);
            }

            var selectedGroup = groups.ElementAt(groupIndex - 1);
            context["SelectedGroupId"] = selectedGroup.Id.ToString();
            context["TransactionPage"] = "1";

            return await LoadTransactionPageAsync(selectedGroup.Id, 1, language, cancellationToken);
        }

        // Step 2: Pagination
        if (session.CurrentScreen == "ViewTransactions_List")
        {
            if (userInput == "9") // Next page
            {
                var groupId = Guid.Parse(context["SelectedGroupId"]);
                var currentPage = int.Parse(context.GetValueOrDefault("TransactionPage", "1"));
                var nextPage = currentPage + 1;
                context["TransactionPage"] = nextPage.ToString();

                return await LoadTransactionPageAsync(groupId, nextPage, language, cancellationToken);
            }
        }

        return ("Error", "MainMenu", context);
    }

    private async Task<(string ResponseText, string NextScreen, Dictionary<string, string> Context)> LoadTransactionPageAsync(
        Guid groupId,
        int page,
        string language,
        CancellationToken cancellationToken)
    {
        // Stub: Get recent contributions for display
        var transactionList = new List<string>
        {
            "R500.00 - 15 Mar",
            "R500.00 - 01 Mar",
            "R500.00 - 15 Feb",
            "R500.00 - 01 Feb",
            "R500.00 - 15 Jan"
        };

        var menu = _menuBuilder.BuildTransactionHistoryMenu(transactionList, language, page);
        return (menu, "ViewTransactions_List", new Dictionary<string, string> { ["TransactionPage"] = page.ToString(), ["SelectedGroupId"] = groupId.ToString() });
    }

    /// <summary>
    /// Validates bank PIN for USSD payments (T105)
    /// </summary>
    private async Task<bool> ValidateBankPINAsync(string memberId, string pin, CancellationToken cancellationToken)
    {
        // STUB: In production, call bank's PIN validation API
        _logger.LogInformation("[STUB] Validating bank PIN for member {MemberId}", memberId);
        await Task.Delay(100, cancellationToken);

        // For MVP: Accept any 4-digit PIN
        return pin.Length == 4 && pin.All(char.IsDigit);
    }
}
