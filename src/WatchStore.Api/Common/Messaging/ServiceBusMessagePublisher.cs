namespace WatchStore.Api.Common.Messaging;

internal sealed class ServiceBusMessagePublisher(
    ServiceBusClient serviceBusClient,
    ILogger<ServiceBusMessagePublisher> logger
) : IMessagePublisher
{
    public async Task PublishAsync<T>(
        T message,
        string queueName,
        string? messageId = null,
        string? correlationId = null
    )
    {
        var messageBody = JsonSerializer.Serialize(message);

        await PublishAsync(
            typeof(T).Name,
            messageBody,
            queueName,
            messageId,
            correlationId
        );
    }

    public async Task PublishAsync(
        string messageType,
        string messageBody,
        string queueName,
        string? messageId = null,
        string? correlationId = null
    )
    {
        try
        {
            await using var sender = serviceBusClient.CreateSender(queueName);

            ServiceBusMessage serviceBusMessage = new(messageBody)
            {
                Subject = messageType ?? "Unknown",
                ContentType = "application/json",
                MessageId = messageId ?? Guid.NewGuid().ToString(),
                CorrelationId = correlationId
            };

            serviceBusMessage.ApplicationProperties["MessageType"] = messageType ?? "Unknown";

            await sender.SendMessageAsync(serviceBusMessage);

            logger.LogInformation("Published {MessageType} to {Queue} (MessageId={MessageId})",
                                    messageType ?? "Unknown",
                                    queueName,
                                    serviceBusMessage.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish {MessageType} to {Queue}",
                                messageType ?? "Unknown",
                                queueName);

            throw;
        }
    }
}
