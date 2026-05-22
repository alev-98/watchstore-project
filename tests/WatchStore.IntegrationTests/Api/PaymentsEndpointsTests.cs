namespace WatchStore.IntegrationTests.Api;

public class PaymentsEndpointsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:18.3").Build();

    private readonly Fixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string PaymentsEndpoint => "/" + PaymentsModule.GroupName;

    public async ValueTask InitializeAsync()
    {
        await postgres.StartAsync();
        fixture.Customize<DateOnly>(o => o.FromFactory(() => DateOnly.FromDateTime(DateTime.UtcNow)));
        fixture.Customize<DateTimeOffset>(o => o.FromFactory(() => DateTimeOffset.UtcNow));
    }

    #region POST /payments/checkout

    [Fact]
    public async Task CreateCheckoutSession_WithValidBasket_CreatesSessionAndPersistsPendingOrder()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        WatchStoreWebApplicationFactory factory = new(postgres, userId: userId.ToString(), scope: Scopes.ApiAccessScope);

        HttpClient client = factory.CreateClient();

        WatchStoreContext db = factory.CreateDbContext();

        Watch watch = Watch.Create(fixture, db);

        CustomerBasket basket = new()
        {
            Id = userId,
            Items =
            [
                new() { WatchId = watch.Id, Quantity = 1 }
            ]
        };

        db.Watches.Add(watch);
        db.Baskets.Add(basket);
        await db.SaveChangesAsync(CancellationToken);

        Guid operationId = Guid.NewGuid();

        CreateCheckoutSessionDto request = new(operationId);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"{PaymentsEndpoint}/checkout", request, CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var responseDto = await response.Content.ReadFromJsonAsync<CheckoutSessionDto>(CancellationToken);
        responseDto!.ClientSecret.ShouldNotBeNullOrEmpty();
        responseDto!.OrderId.ShouldNotBe(Guid.Empty);

        WatchStoreContext verifyDb = factory.CreateDbContext();

        Order? order = await verifyDb.Orders.FindAsync([responseDto.OrderId], CancellationToken);

        order.ShouldNotBeNull();
        order!.CustomerId.ShouldBe(userId);
        order.OperationId.ShouldBe(operationId);
        order.Status.ShouldBe(OrderStatus.Pending);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithSameOperationId_ReturnsSameOrderId()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        WatchStoreWebApplicationFactory factory = new(postgres, userId: userId.ToString(), scope: Scopes.ApiAccessScope);

        HttpClient client = factory.CreateClient();

        WatchStoreContext db = factory.CreateDbContext();

        Watch watch = Watch.Create(fixture, db);

        CustomerBasket basket = new()
        {
            Id = userId,
            Items =
            [
                new() { WatchId = watch.Id, Quantity = 1 }
            ]
        };

        db.Watches.Add(watch);
        db.Baskets.Add(basket);
        await db.SaveChangesAsync(CancellationToken);

        Guid operationId = Guid.NewGuid();

        CreateCheckoutSessionDto request = new(operationId);

        // Act
        HttpResponseMessage response1 = await client.PostAsJsonAsync($"{PaymentsEndpoint}/checkout", request, CancellationToken);
        var dto1 = await response1.Content.ReadFromJsonAsync<CheckoutSessionDto>(CancellationToken);

        HttpResponseMessage response2 = await client.PostAsJsonAsync($"{PaymentsEndpoint}/checkout", request, CancellationToken);
        var dto2 = await response2.Content.ReadFromJsonAsync<CheckoutSessionDto>(CancellationToken);

        // Assert
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();

        dto1.ShouldNotBeNull();
        dto2.ShouldNotBeNull();

        dto1!.OrderId.ShouldNotBe(Guid.Empty);
        dto2!.OrderId.ShouldBe(dto1.OrderId);

        // Assert
        WatchStoreContext verifyDb = factory.CreateDbContext();

        List<Order> orders = await verifyDb.Orders
                                            .Where(o => o.OperationId == operationId)
                                            .ToListAsync(CancellationToken);

        orders.Count.ShouldBe(1);
        orders[0].Id.ShouldBe(dto1.OrderId);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithEmptyBasket_ReturnsBadRequest()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        WatchStoreWebApplicationFactory factory = new(postgres, userId: userId.ToString(), scope: Scopes.ApiAccessScope);

        HttpClient client = factory.CreateClient();

        Guid operationId = Guid.NewGuid();

        CreateCheckoutSessionDto request = new(operationId);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"{PaymentsEndpoint}/checkout", request, CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCheckoutSession_WithValidBasket_SetsCorrectLineItemOnStripeCheckoutSession()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        WatchStoreWebApplicationFactory factory = new(postgres, userId: userId.ToString(), scope: Scopes.ApiAccessScope);

        HttpClient client = factory.CreateClient();

        WatchStoreContext db = factory.CreateDbContext();

        Watch watch = Watch.Create(fixture, db);

        decimal price = watch.Price;

        CustomerBasket basket = new()
        {
            Id = userId,
            Items =
            [
                new() { WatchId = watch.Id, Quantity = 1 }
            ]
        };

        db.Watches.Add(watch);
        db.Baskets.Add(basket);
        await db.SaveChangesAsync(CancellationToken);

        Guid operationId = Guid.NewGuid();

        CreateCheckoutSessionDto request = new(operationId);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"{PaymentsEndpoint}/checkout", request, CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        long expectedCents = (long)(price * 100m);

        var sessionService = factory.Services.GetRequiredService<SessionService>();

        await sessionService.Received(1).CreateAsync(
            Arg.Is<SessionCreateOptions>(o =>
                o.LineItems.Count == 1 &&
                o.LineItems[0].PriceData.UnitAmount == expectedCents &&
                o.LineItems[0].Quantity == 1),
            Arg.Any<RequestOptions>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region POST/payments/stripe-webhook

    [Fact]
    public async Task StripeWebhook_WithCheckoutSessionCompleted_UpdatesOrderStatusAndCreatesOutboxMessage()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        IStripeEventFactory stripeEventFactoryStub = Substitute.For<IStripeEventFactory>();

        WatchStoreWebApplicationFactory factory = new(
                                                    postgres,
                                                    userId: userId.ToString(),
                                                    stripeEventFactoryOverride: stripeEventFactoryStub);

        WatchStoreContext db = factory.CreateDbContext();

        Order order = fixture.Build<Order>()
            .With(o => o.Id, Guid.NewGuid())
            .With(o => o.CustomerId, userId)
            .With(o => o.Status, OrderStatus.Pending)
            .With(o => o.PaymentId, (string?)null)
            .With(o => o.PaymentCardBrand, (string?)null)
            .With(o => o.PaymentCardLast4, (string?)null)
            .Create();

        db.Orders.Add(order);

        await db.SaveChangesAsync(CancellationToken);

        string paymentIntentId = Guid.NewGuid().ToString();

        Session session = new()
        {
            Id = Guid.NewGuid().ToString(),
            Object = "checkout.session",
            PaymentIntentId = paymentIntentId,
            AmountTotal = 5000,
            Metadata = new Dictionary<string, string>
            {
                [MetadataKeys.OrderId] = order.Id.ToString()
            }
        };

        Event evt = new()
        {
            Id = Guid.NewGuid().ToString(),
            Type = EventTypes.CheckoutSessionCompleted,
            Data = new Stripe.EventData { Object = session }
        };

        stripeEventFactoryStub.Create(Arg.Any<string>(), Arg.Any<string>()).Returns(evt);

        PaymentIntent paymentIntent = new()
        {
            Id = paymentIntentId,
            PaymentMethod = new PaymentMethod
            {
                Id = "pm_test",
                Card = new PaymentMethodCard
                {
                    Brand = "visa",
                    Last4 = "4242"
                }
            }
        };

        PaymentIntentService paymentIntentService = factory.Services.GetRequiredService<PaymentIntentService>();

        paymentIntentService.GetAsync(
            paymentIntentId,
            Arg.Any<PaymentIntentGetOptions>(),
            Arg.Any<RequestOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(paymentIntent));

        HttpClient client = factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"{PaymentsEndpoint}/stripe-webhook", evt, CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        WatchStoreContext verifyDb = factory.CreateDbContext();

        Order updated = await verifyDb.Orders
                                        .AsNoTracking()
                                        .SingleAsync(o => o.Id == order.Id, CancellationToken);

        updated.Status.ShouldBe(OrderStatus.Processing);
        updated.PaymentId.ShouldBe(paymentIntentId);
        updated.PaymentCardBrand.ShouldBe("visa");
        updated.PaymentCardLast4.ShouldBe("4242");

        OutboxMessage? outboxMessage = await verifyDb.OutboxMessages
                                                        .AsNoTracking()
                                                        .SingleOrDefaultAsync(
                                                            m => m.CorrelationId == order.Id.ToString(),
                                                            CancellationToken);

        outboxMessage.ShouldNotBeNull();

        outboxMessage!.MessageType.ShouldBe("OrderPaid");
        outboxMessage.QueueName.ShouldBe("orders");
        outboxMessage.MessageId.ShouldBe(paymentIntentId);
        outboxMessage.ProcessedAt.ShouldBeNull();
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        await postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
