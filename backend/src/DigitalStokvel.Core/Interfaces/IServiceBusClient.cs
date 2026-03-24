namespace DigitalStokvel.Core.Interfaces;

/// <summary>
/// Interface for Azure Service Bus messaging operations
/// </summary>
public interface IServiceBusClient : IAsyncDisposable
{
    /// <summary>
    /// Sends a notification message to the Service Bus queue
    /// </summary>
    Task SendNotificationAsync(
        string messageBody,
        string messageType,
        Dictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default);
}
