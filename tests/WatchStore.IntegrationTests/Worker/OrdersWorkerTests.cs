namespace WatchStore.IntegrationTests.Worker;

public class OrdersWorkerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgreContainer = new PostgreSqlBuilder("postgres:18.3").Build();

    private ServiceBusContainer? serviceBusContainer;

    private readonly Fixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await postgreContainer.StartAsync(CancellationToken);

        fixture.Customize<DateTimeOffset>(o => o.FromFactory(() => DateTimeOffset.UtcNow));

        string configFile = Path.Combine(AppContext.BaseDirectory, "Worker", "servicebus.config.json");

        serviceBusContainer = new ServiceBusBuilder("mcr.microsoft.com/azure-messaging/servicebus-emulator:2.0.0")
                                    .WithAcceptLicenseAgreement(true)
                                    .WithConfig(configFile)
                                    .Build();

        await serviceBusContainer.StartAsync(CancellationToken);
    }

    [Fact]
    public async Task Consume_OrderPaid_CompletesOrder()
    {
        string dbConnString = postgreContainer.GetConnectionString();
        string sbConnString = serviceBusContainer!.GetConnectionString();

        var dbOptions = new DbContextOptionsBuilder<WatchStoreContext>()
                            .UseNpgsql(dbConnString)
                            .Options;

        Guid orderId = Guid.NewGuid();

        // Aggiunge ordine
        await using (WatchStoreContext setupDb = new(dbOptions))
        {
            await setupDb.Database.MigrateAsync(CancellationToken);

            Order order = fixture.Build<Order>()
                .With(o => o.Id, orderId)
                .With(o => o.Status, OrderStatus.Processing)
                .Without(o => o.Items)
                .Create();

            setupDb.Orders.Add(order);

            await setupDb.SaveChangesAsync(CancellationToken);
        }

        // Crea host
        IHost host = WorkerHostFactory.Build(
            environmentName: "Testing",
            configure: configBuilder =>
            {
                Dictionary<string, string?> overrides = new()
                {
                    ["ConnectionStrings:WatchStoreDB"] = dbConnString,
                    ["ConnectionStrings:serviceBus"] = sbConnString
                };
                configBuilder.AddInMemoryCollection(overrides);
            }
        );

        await host.StartAsync(CancellationToken);

        // Spedisce il messaggio
        await using (var client = new ServiceBusClient(sbConnString))
        {
            ServiceBusMessagePublisher publisher = new(client, NullLogger<ServiceBusMessagePublisher>.Instance);

            await publisher.PublishAsync(new OrderPaid(orderId), queueName: QueueNames.Orders);
        }

        Order? orderAfter = null;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // Controlal se il messaggio è stato completato
        while (!cts.IsCancellationRequested)
        {
            await using var db = new WatchStoreContext(dbOptions);

            orderAfter = await db.Orders
                            .AsNoTracking()
                            .FirstOrDefaultAsync(o => o.Id == orderId, cts.Token);

            if (orderAfter?.Status == OrderStatus.Completed)
            {
                break;
            }

            await Task.Delay(200, cts.Token);
        }

        orderAfter.ShouldNotBeNull();
        orderAfter!.Status.ShouldBe(OrderStatus.Completed);

        await host.StopAsync(CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await postgreContainer.DisposeAsync();
        if (serviceBusContainer is not null)
        {
            await serviceBusContainer.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }
}