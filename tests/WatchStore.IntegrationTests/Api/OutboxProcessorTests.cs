namespace WatchStore.IntegrationTests.Api;

public class OutboxProcessorTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:18.3").Build();

    private readonly Fixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await postgres.StartAsync();
        fixture.Customize<DateTimeOffset>(o => o.FromFactory(() => DateTimeOffset.UtcNow));
    }

    [Fact]
    public async Task OutboxProcessor_WithPendingMessage_PublishesAndMarksAsProcessed()
    {
        // Arrange
        IMessagePublisher messagePublisherStub = Substitute.For<IMessagePublisher>();

        // Creazione messaggio per seeding nel db direttamente
        Guid orderId = Guid.NewGuid();
        string paymentId = Guid.NewGuid().ToString();
        OrderPaid orderPaidMessage = new(orderId);

        OutboxMessage outboxMessage = new()
        {
            MessageType = nameof(OrderPaid),
            QueueName = QueueNames.Orders,
            Payload = JsonSerializer.Serialize(orderPaidMessage),
            MessageId = paymentId,
            CorrelationId = orderId.ToString(),
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null,
            RetryCount = 0
        };

        // Seed per id
        WatchStoreWebApplicationFactory setupFactory = new(postgres, disableBackgroundServices: true);

        WatchStoreContext db = setupFactory.CreateDbContext();

        db.OutboxMessages.Add(outboxMessage);

        await db.SaveChangesAsync(CancellationToken);

        // Interceptor per quando il messaggio verrà processato
        Guid messageId = outboxMessage.Id;

        OutboxMessageProcessedInterceptor probe = new(messageId);

        WatchStoreWebApplicationFactory factoryWithStub = new(
                                                            postgres,
                                                            saveChangesInterceptor: probe,
                                                            messagePublisher: messagePublisherStub);

        // Act
        factoryWithStub.StartServer();

        await probe.WaitAsync(TimeSpan.FromSeconds(30));

        // Assert
        await messagePublisherStub.Received(1).PublishAsync(
                                                nameof(OrderPaid),
                                                Arg.Is<string>(payload => payload.Contains(orderId.ToString())),
                                                QueueNames.Orders,
                                                paymentId,
                                                orderId.ToString());

        WatchStoreContext verifyDb = factoryWithStub.CreateDbContext();

        OutboxMessage processedMessage = await verifyDb.OutboxMessages
                                                        .AsNoTracking()
                                                        .FirstAsync(m => m.Id == messageId, CancellationToken);

        processedMessage.ProcessedAt.ShouldNotBeNull();
        processedMessage.LastError.ShouldBeNull();
        processedMessage.RetryCount.ShouldBe(0);
    }

    public async ValueTask DisposeAsync()
    {
        await postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
