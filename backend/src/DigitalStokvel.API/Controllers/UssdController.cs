using DigitalStokvel.Infrastructure.USSD;
using DigitalStokvel.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalStokvel.API.Controllers;

/// <summary>
/// USSD webhook endpoint for receiving MNO callbacks (T104)
/// Handles incoming USSD requests from Vodacom, MTN, Cell C, and Telkom
/// </summary>
[ApiController]
[Route("api/v1/ussd")]
public class UssdController : ControllerBase
{
    private readonly UssdSessionManager _sessionManager;
    private readonly UssdMenuBuilder _menuBuilder;
    private readonly UssdFlowService _flowService;
    private readonly ILogger<UssdController> _logger;

    public UssdController(
        UssdSessionManager sessionManager,
        UssdMenuBuilder menuBuilder,
        UssdFlowService flowService,
        ILogger<UssdController> logger)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _menuBuilder = menuBuilder ?? throw new ArgumentNullException(nameof(menuBuilder));
        _flowService = flowService ?? throw new ArgumentNullException(nameof(flowService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Webhook endpoint for MNO USSD callbacks
    /// </summary>
    /// <param name="request">USSD request from MNO</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>USSD response to send back to user</returns>
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleUssdWebhook(
        [FromBody] UssdWebhookRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "USSD webhook received | SessionId: {SessionId} | Phone: {PhoneNumber} | Input: {Input}",
                request.SessionId, request.PhoneNumber, request.UserInput);

            // Retrieve or create session (T103: Session restoration within 120s)
            var session = await _sessionManager.GetSessionAsync(request.SessionId, cancellationToken);
            
            if (session == null)
            {
                // New session - show main menu
                session = new UssdSessionState
                {
                    SessionId = request.SessionId,
                    PhoneNumber = request.PhoneNumber,
                    Language = request.Language ?? "en",
                    CurrentScreen = "MainMenu",
                    MenuDepth = 0,
                    Context = new Dictionary<string, string>
                    {
                        ["MemberId"] = await GetMemberIdByPhoneAsync(request.PhoneNumber, cancellationToken)
                    }
                };

                var mainMenu = _menuBuilder.BuildMainMenu(session.Language);
                await _sessionManager.SaveSessionAsync(request.SessionId, session, cancellationToken);

                return Ok(new UssdWebhookResponse
                {
                    SessionId = request.SessionId,
                    Message = mainMenu,
                    ContinueSession = true
                });
            }

            // Process user input based on current screen
            var (responseText, nextScreen, updatedContext) = await ProcessUserInputAsync(session, request.UserInput, cancellationToken);

            // Update session
            session.CurrentScreen = nextScreen;
            session.Context = updatedContext ?? session.Context;
            session.LastActivityAt = DateTime.UtcNow;
            session.MenuDepth++;

            // Validate menu depth (T107)
            var (isValid, depthError) = _menuBuilder.ValidateMenuDepth(session.MenuDepth, nextScreen);
            if (!isValid)
            {
                _logger.LogWarning("Menu depth validation failed: {Error}", depthError);
                responseText = "Menu navigation error. Starting over...";
                nextScreen = "MainMenu";
                session.MenuDepth = 0;
            }

            await _sessionManager.SaveSessionAsync(request.SessionId, session, cancellationToken);

            // Determine if session should continue
            var continueSession = nextScreen != "MainMenu" && !nextScreen.Contains("Display") && !nextScreen.Contains("Receipt");

            return Ok(new UssdWebhookResponse
            {
                SessionId = request.SessionId,
                Message = responseText,
                ContinueSession = continueSession
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "USSD webhook processing failed for session {SessionId}", request.SessionId);
            
            return Ok(new UssdWebhookResponse
            {
                SessionId = request.SessionId,
                Message = "An error occurred. Please try again.",
                ContinueSession = false
            });
        }
    }

    private async Task<(string ResponseText, string NextScreen, Dictionary<string, string>? UpdatedContext)> ProcessUserInputAsync(
        UssdSessionState session,
        string userInput,
        CancellationToken cancellationToken)
    {
        // Main Menu routing
        if (session.CurrentScreen == "MainMenu")
        {
            return userInput switch
            {
                "1" => (await GetGroupSelectionMenuAsync(session, cancellationToken), "PayContribution_SelectGroup", null),
                "2" => (await GetGroupSelectionMenuAsync(session, cancellationToken), "CheckBalance_SelectGroup", null),
                "3" => (await GetGroupSelectionMenuAsync(session, cancellationToken), "ViewTransactions_SelectGroup", null),
                "4" => (_menuBuilder.BuildLanguageMenu(session.Language), "LanguageSelection", null),
                _ => (_menuBuilder.BuildMainMenu(session.Language), "MainMenu", null)
            };
        }

        // Payment flow
        if (session.CurrentScreen.StartsWith("PayContribution"))
        {
            var result = await _flowService.ProcessPayContributionFlowAsync(session, userInput, cancellationToken);
            return (result.ResponseText, result.NextScreen, result.UpdatedContext);
        }

        // Balance flow
        if (session.CurrentScreen.StartsWith("CheckBalance"))
        {
            var result = await _flowService.ProcessCheckBalanceFlowAsync(session, userInput, cancellationToken);
            return (result.ResponseText, result.NextScreen, null);
        }

        // Transactions flow
        if (session.CurrentScreen.StartsWith("ViewTransactions"))
        {
            var result = await _flowService.ProcessViewTransactionsFlowAsync(session, userInput, cancellationToken);
            return (result.ResponseText, result.NextScreen, result.UpdatedContext);
        }

        return (_menuBuilder.BuildMainMenu(session.Language), "MainMenu", null);
    }

    private async Task<string> GetGroupSelectionMenuAsync(UssdSessionState session, CancellationToken cancellationToken)
    {
        // Stub: In production, fetch actual groups from repository
        var groupNames = new List<string> { "Family Stokvel", "Church Group", "Saving Circle" };
        return _menuBuilder.BuildGroupSelectionMenu(groupNames, session.Language);
    }

    private async Task<string> GetMemberIdByPhoneAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        // Stub: In production, lookup member by phone number
        _logger.LogInformation("[STUB] Looking up member by phone: {PhoneNumber}", phoneNumber);
        await Task.Delay(10, cancellationToken);
        return Guid.NewGuid().ToString();
    }
}

/// <summary>
/// USSD webhook request from MNO
/// </summary>
public class UssdWebhookRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string UserInput { get; set; } = string.Empty;
    public string? Language { get; set; }
    public string? Provider { get; set; }
}

/// <summary>
/// USSD webhook response to MNO
/// </summary>
public class UssdWebhookResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool ContinueSession { get; set; }
}
