using Azure.Messaging.ServiceBus;
using DigitalStokvel.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DigitalStokvel.Infrastructure.Messaging;

/// <summary>
/// Azure Service Bus client for async notification delivery
/// </summary>
public class ServiceBusClient : IServiceBusClient
{
    private readonly Azure.Messaging.ServiceBus.ServiceBusClient _client;
    private readonly ServiceBusSender _notificationSender;
    private readonly ILogger<ServiceBusClient> _logger;

    public ServiceBusClient(string connectionString, ILogger<ServiceBusClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Service Bus connection string is required", nameof(connectionString));
        }

        _client = new Azure.Messaging.ServiceBus.ServiceBusClient(connectionString);
        _notificationSender = _client.CreateSender("notifications"); // Queue name
    }

    /// <summary>
    /// Sends a notification message to the Service Bus queue
    /// </summary>
    public async Task SendNotificationAsync(
        string messageBody,
        string messageType,
        Dictionary<string, object>? properties = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new ServiceBusMessage(messageBody)
            {
                ContentType = "application/json",
                Subject = messageType
            };

            // Add custom properties
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    message.ApplicationProperties[prop.Key] = prop.Value;
                }
            }

            await _notificationSender.SendMessageAsync(message, cancellationToken);

            _logger.LogInformation("Notification sent to Service Bus: {MessageType}", messageType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to Service Bus: {MessageType}", messageType);
            throw;
        }
    }

    /// <summary>
    /// Sends multiple notification messages in a batch
    /// </summary>
    public async Task SendNotificationBatchAsync(
        IEnumerable<(string body, string type)> messages,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var messageBatch = await _notificationSender.CreateMessageBatchAsync(cancellationToken);

            foreach (var (body, type) in messages)
            {
                var message = new ServiceBusMessage(body)
                {
                    ContentType = "application/json",
                    Subject = type
                };

                if (!messageBatch.TryAddMessage(message))
                {
                    // If batch is full, send it and create a new batch
                    await _notificationSender.SendMessagesAsync(messageBatch, cancellationToken);
                    messageBatch.Dispose();

                    var newBatch = await _notificationSender.CreateMessageBatchAsync(cancellationToken);
                    newBatch.TryAddMessage(message);
                }
            }

            if (messageBatch.Count > 0)
            {
                await _notificationSender.SendMessagesAsync(messageBatch, cancellationToken);
            }

            _logger.LogInformation("Batch of {Count} notifications sent to Service Bus", messageBatch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification batch to Service Bus");
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _notificationSender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
