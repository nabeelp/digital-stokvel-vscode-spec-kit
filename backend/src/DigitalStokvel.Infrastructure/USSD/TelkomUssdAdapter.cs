using DigitalStokvel.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DigitalStokvel.Infrastructure.USSD;

/// <summary>
/// USSD gateway adapter for Telkom network (stub implementation for MVP)
/// </summary>
public class TelkomUssdAdapter : IUssdGateway
{
    private readonly ILogger<TelkomUssdAdapter> _logger;
    public string ProviderName => "Telkom";

    public TelkomUssdAdapter(ILogger<TelkomUssdAdapter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(bool Success, string? ErrorMessage)> SendMenuAsync(string sessionId, string phoneNumber, string menuText, bool expectsResponse, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[TELKOM STUB] Sending USSD menu | Session: {SessionId}", sessionId);
        await Task.Delay(50, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? ResponseText, bool ContinueSession, string? ErrorMessage)> ProcessInputAsync(string sessionId, string phoneNumber, string userInput, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[TELKOM STUB] Processing input | Session: {SessionId} | Input: {UserInput}", sessionId, userInput);
        await Task.Delay(50, cancellationToken);
        return (true, null, true, null);
    }

    public async Task<(bool Success, string? SessionData, string? ErrorMessage)> ManageSessionAsync(string sessionId, string phoneNumber, string operation, string? sessionData = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[TELKOM STUB] Managing session | Session: {SessionId} | Operation: {Operation}", sessionId, operation);
        await Task.Delay(20, cancellationToken);
        return operation.ToLower() switch
        {
            "save" => (true, null, null),
            "retrieve" => (true, sessionData, null),
            "expire" => (true, null, null),
            _ => (false, null, "Invalid operation")
        };
    }
}
