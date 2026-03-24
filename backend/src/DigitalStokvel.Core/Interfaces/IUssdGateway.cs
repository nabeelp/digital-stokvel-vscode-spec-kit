namespace DigitalStokvel.Core.Interfaces;

/// <summary>
/// Interface for USSD gateway integration with Mobile Network Operators (MNOs)
/// Supports Vodacom, MTN, Cell C, and Telkom USSD services
/// </summary>
public interface IUssdGateway
{
    /// <summary>
    /// Sends a USSD menu to the user's phone
    /// </summary>
    /// <param name="sessionId">Unique session identifier from MNO</param>
    /// <param name="phoneNumber">User's phone number in E.164 format (+27...)</param>
    /// <param name="menuText">Text content to display (max 160 characters)</param>
    /// <param name="expectsResponse">True if waiting for user input, false for final message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if menu sent successfully</returns>
    Task<(bool Success, string? ErrorMessage)> SendMenuAsync(
        string sessionId,
        string phoneNumber,
        string menuText,
        bool expectsResponse,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes user input received from USSD session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="phoneNumber">User's phone number</param>
    /// <param name="userInput">User's menu selection or text input</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response text to send back to user</returns>
    Task<(bool Success, string? ResponseText, bool ContinueSession, string? ErrorMessage)> ProcessInputAsync(
        string sessionId,
        string phoneNumber,
        string userInput,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manages session state (save/retrieve/expire)
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="phoneNumber">User's phone number</param>
    /// <param name="operation">Operation type: "save", "retrieve", "expire"</param>
    /// <param name="sessionData">Session state data (for save operation)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session data for retrieve operation, or success status for save/expire</returns>
    Task<(bool Success, string? SessionData, string? ErrorMessage)> ManageSessionAsync(
        string sessionId,
        string phoneNumber,
        string operation,
        string? sessionData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the MNO provider name (e.g., "Vodacom", "MTN", "Cell C", "Telkom")
    /// </summary>
    string ProviderName { get; }
}
