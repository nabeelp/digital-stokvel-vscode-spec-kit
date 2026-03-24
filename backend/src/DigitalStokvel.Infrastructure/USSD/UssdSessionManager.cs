using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DigitalStokvel.Infrastructure.USSD;

/// <summary>
/// Manages USSD session state with 120-second persistence using Redis
/// </summary>
public class UssdSessionManager
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<UssdSessionManager> _logger;
    private const int SessionTimeoutSeconds = 120;

    public UssdSessionManager(
        IDistributedCache cache,
        ILogger<UssdSessionManager> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Saves session state to Redis with 120-second expiration
    /// </summary>
    public async Task<bool> SaveSessionAsync(string sessionId, UssdSessionState state, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetSessionKey(sessionId);
            var json = JsonSerializer.Serialize(state);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(SessionTimeoutSeconds)
            };

            await _cache.SetStringAsync(key, json, options, cancellationToken);

            _logger.LogInformation(
                "USSD session saved | SessionId: {SessionId} | Screen: {CurrentScreen} | Depth: {MenuDepth}",
                sessionId, state.CurrentScreen, state.MenuDepth);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save USSD session {SessionId}", sessionId);
            return false;
        }
    }

    /// <summary>
    /// Retrieves session state from Redis
    /// </summary>
    public async Task<UssdSessionState?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetSessionKey(sessionId);
            var json = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(json))
            {
                _logger.LogWarning("USSD session not found or expired: {SessionId}", sessionId);
                return null;
            }

            var state = JsonSerializer.Deserialize<UssdSessionState>(json);
            _logger.LogInformation("USSD session retrieved | SessionId: {SessionId}", sessionId);

            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve USSD session {SessionId}", sessionId);
            return null;
        }
    }

    /// <summary>
    /// Extends session expiration by another 120 seconds (for session restoration)
    /// </summary>
    public async Task<bool> ExtendSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var state = await GetSessionAsync(sessionId, cancellationToken);
            if (state == null)
            {
                return false;
            }

            // Re-save with fresh 120-second timeout
            return await SaveSessionAsync(sessionId, state, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extend USSD session {SessionId}", sessionId);
            return false;
        }
    }

    /// <summary>
    /// Expires (deletes) a session immediately
    /// </summary>
    public async Task<bool> ExpireSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = GetSessionKey(sessionId);
            await _cache.RemoveAsync(key, cancellationToken);

            _logger.LogInformation("USSD session expired | SessionId: {SessionId}", sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to expire USSD session {SessionId}", sessionId);
            return false;
        }
    }

    private string GetSessionKey(string sessionId) => $"ussd:session:{sessionId}";
}

/// <summary>
/// Represents USSD session state stored in Redis
/// </summary>
public class UssdSessionState
{
    public string SessionId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public string CurrentScreen { get; set; } = "MainMenu";
    public int MenuDepth { get; set; } = 0;
    public Dictionary<string, string> Context { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
}
