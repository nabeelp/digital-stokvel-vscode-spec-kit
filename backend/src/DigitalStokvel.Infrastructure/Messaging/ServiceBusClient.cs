using Azure.Messaging.ServiceBus;
using DigitalStokvel.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DigitalStokvel.Infrastructure.Messaging;

/// <summary>
/// Azure Service Bus client for async notification delivery with dead letter queue handling (T200)
/// </summary>
public class ServiceBusClient : IServiceBusClient
{
    private readonly Azure.Messaging.ServiceBus.ServiceBusClient _client;
    private readonly ServiceBusSender _notificationSender;
    private readonly ServiceBusReceiver? _deadLetterReceiver;
    private readonly ILogger<ServiceBusClient> _logger;
    private readonly string _queueName = "notifications";

    public ServiceBusClient(string connectionString, ILogger<ServiceBusClient> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Service Bus connection string is required", nameof(connectionString));
        }

        _client = new Azure.Messaging.ServiceBus.ServiceBusClient(connectionString);
        _notificationSender = _client.CreateSender(_queueName);
        
        // T200: Create receiver for dead letter sub-queue
        try
        {
            _deadLetterReceiver = _client.CreateReceiver(_queueName, new ServiceBusReceiverOptions
            {
                SubQueue = SubQueue.DeadLetter
            });
            _logger.LogInformation("Dead letter queue receiver initialized for queue: {QueueName}", _queueName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize dead letter queue receiver. Dead letter handling will be unavailable.");
        }
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

    /// <summary>
    /// T200: Process messages from the dead letter queue
    /// Returns the number of messages processed and resubmitted
    /// </summary>
    public async Task<(int ProcessedCount, int ResubmittedCount, int FailedCount)> ProcessDeadLetterMessagesAsync(
        int maxMessages = 10,
        CancellationToken cancellationToken = default)
    {
        if (_deadLetterReceiver == null)
        {
            _logger.LogWarning("Dead letter receiver not initialized. Cannot process dead letter messages.");
            return (0, 0, 0);
        }

        int processedCount = 0;
        int resubmittedCount = 0;
        int failedCount = 0;

        try
        {
            _logger.LogInformation("Processing dead letter messages (max: {MaxMessages})", maxMessages);

            // Receive messages from dead letter queue
            var messages = await _deadLetterReceiver.ReceiveMessagesAsync(
                maxMessages,
                TimeSpan.FromSeconds(5),
                cancellationToken);

            foreach (var message in messages)
            {
                processedCount++;

                try
                {
                    // Log dead letter reason
                    var deadLetterReason = message.DeadLetterReason ?? "Unknown";
                    var deadLetterErrorDescription = message.DeadLetterErrorDescription ?? "No description";
                    var deliveryCount = message.DeliveryCount;

                    _logger.LogWarning(
                        "Dead letter message: Reason={Reason}, Description={Description}, DeliveryCount={Count}, MessageId={MessageId}",
                        deadLetterReason,
                        deadLetterErrorDescription,
                        deliveryCount,
                        message.MessageId);

                    // Determine if message should be retried
                    var shouldRetry = ShouldRetryDeadLetterMessage(message, deadLetterReason, deliveryCount);

                    if (shouldRetry)
                    {
                        // Resubmit to main queue
                        var resubmitMessage = new ServiceBusMessage(message.Body)
                        {
                            ContentType = message.ContentType,
                            Subject = message.Subject,
                            MessageId = $"{message.MessageId}-retry-{DateTime.UtcNow:yyyyMMddHHmmss}"
                        };

                        // Copy application properties
                        foreach (var prop in message.ApplicationProperties)
                        {
                            resubmitMessage.ApplicationProperties[prop.Key] = prop.Value;
                        }

                        // Add retry metadata
                        resubmitMessage.ApplicationProperties["OriginalMessageId"] = message.MessageId;
                        resubmitMessage.ApplicationProperties["RetryAttempt"] = deliveryCount;
                        resubmitMessage.ApplicationProperties["DeadLetterReason"] = deadLetterReason;

                        await _notificationSender.SendMessageAsync(resubmitMessage, cancellationToken);
                        resubmittedCount++;

                        _logger.LogInformation(
                            "Resubmitted dead letter message {MessageId} to main queue",
                            message.MessageId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Dead letter message {MessageId} not eligible for retry. Reason: {Reason}, DeliveryCount: {Count}",
                            message.MessageId,
                            deadLetterReason,
                            deliveryCount);
                    }

                    // Complete the dead letter message (remove from DLQ)
                    await _deadLetterReceiver.CompleteMessageAsync(message, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process dead letter message {MessageId}", message.MessageId);
                    failedCount++;

                    // Abandon the message so it stays in DLQ for later processing
                    await _deadLetterReceiver.AbandonMessageAsync(message, cancellationToken: cancellationToken);
                }
            }

            _logger.LogInformation(
                "Dead letter processing complete. Processed: {Processed}, Resubmitted: {Resubmitted}, Failed: {Failed}",
                processedCount,
                resubmittedCount,
                failedCount);

            return (processedCount, resubmittedCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing dead letter messages");
            return (processedCount, resubmittedCount, failedCount);
        }
    }

    /// <summary>
    /// T200: Determines if a dead letter message should be retried
    /// </summary>
    private bool ShouldRetryDeadLetterMessage(ServiceBusReceivedMessage message, string deadLetterReason, int deliveryCount)
    {
        // Don't retry if already attempted too many times
        if (deliveryCount > 5)
        {
            _logger.LogWarning("Message {MessageId} has exceeded max delivery count ({Count}). Not retrying.", 
                message.MessageId, deliveryCount);
            return false;
        }

        // Don't retry messages with permanent failures
        var permanentFailureReasons = new[]
        {
            "ValidationError",
            "InvalidMessageFormat",
            "UnauthorizedAccess",
            "ResourceNotFound"
        };

        if (permanentFailureReasons.Any(reason => 
            deadLetterReason.Contains(reason, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Message {MessageId} has permanent failure reason: {Reason}. Not retrying.", 
                message.MessageId, deadLetterReason);
            return false;
        }

        // Retry transient failures
        var transientFailureReasons = new[]
        {
            "ServiceTimeout",
            "ServiceUnavailable",
            "NetworkError",
            "TemporaryFailure"
        };

        if (transientFailureReasons.Any(reason => 
            deadLetterReason.Contains(reason, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogInformation("Message {MessageId} has transient failure reason: {Reason}. Eligible for retry.", 
                message.MessageId, deadLetterReason);
            return true;
        }

        // Default: retry messages with unknown reasons (up to delivery count limit)
        return true;
    }

    /// <summary>
    /// T200: Get count of messages in dead letter queue
    /// </summary>
    public async Task<long> GetDeadLetterMessageCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // In production, this would use ServiceBusAdministrationClient to get queue metrics
            // For MVP, return placeholder
            _logger.LogInformation("Retrieving dead letter message count for queue: {QueueName}", _queueName);
            
            // Stub implementation - production would use:
            // var adminClient = new ServiceBusAdministrationClient(connectionString);
            // var queueProps = await adminClient.GetQueueRuntimePropertiesAsync(_queueName, cancellationToken);
            // return queueProps.Value.DeadLetterMessageCount;
            
            return 0; // Placeholder
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dead letter message count");
            return -1;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_deadLetterReceiver != null)
        {
            await _deadLetterReceiver.DisposeAsync();
        }
        await _notificationSender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
