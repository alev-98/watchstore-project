namespace WatchStore.IntegrationTests.Api;

public class OrdersEndpointsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:18.3").Build();

    private readonly Fixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string OrdersEndpoint => "/" + OrderModule.GroupName;

    public async ValueTask InitializeAsync()
    {
        await postgres.StartAsync();
        fixture.Customize<DateOnly>(o => o.FromFactory(() => DateOnly.FromDateTime(DateTime.UtcNow)));
        fixture.Customize<DateTimeOffset>(o => o.FromFactory(() => DateTimeOffset.UtcNow));
    }

    #region GET /orders/{id}

    [Fact]
    public async Task Get_WithValidOrderId_ReturnsOrderForOwner()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        WatchStoreWebApplicationFactory factory = new(postgres, userId: userId.ToString(), scope: Scopes.ApiAccessScope);

        HttpClient client = factory.CreateClient();

        WatchStoreContext db = factory.CreateDbContext();

        Order order = Order.Create(fixture, userId, OrderStatus.Completed);

        OrderItem orderItem = fixture.Create<OrderItem>();
        order.Items = [orderItem];

        db.Orders.Add(order);
        await db.SaveChangesAsync(CancellationToken);

        // Act
        HttpResponseMessage response = await client.GetAsync($"{OrdersEndpoint}/{order.Id}", CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var orderDto = await response.Content.ReadFromJsonAsync<GetOrder.OrderDto>(CancellationToken);

        orderDto.ShouldNotBeNull();
        orderDto.Id.ShouldBe(order.Id);
        orderDto.CustomerId.ShouldBe(userId);
        orderDto.Status.ShouldBe(order.Status.ToString());
        orderDto.TotalAmount.ShouldBe(order.TotalAmount);
        orderDto.Items.ShouldNotBeNull();
        orderDto.Items.Count().ShouldBe(1);

        var dtoItem = orderDto.Items.First();

        dtoItem.ProductId.ShouldBe(orderItem.ProductId);
        dtoItem.ProductName.ShouldBe(orderItem.ProductName);
        dtoItem.Price.ShouldBe(orderItem.Price);
        dtoItem.Quantity.ShouldBe(orderItem.Quantity);
        dtoItem.ImageUri.ShouldBe(orderItem.ImageUri);
        dtoItem.WatchCodes.ShouldBe(orderItem.WatchCodes);
    }

    [Fact]
    public async Task Get_WithDifferentUserId_ReturnsForbidden()
    {
        // Arrange
        Guid orderOwnerId = Guid.NewGuid();
        Guid differentUserId = Guid.NewGuid();

        WatchStoreWebApplicationFactory factory = new(postgres, userId: differentUserId.ToString(), scope: Scopes.ApiAccessScope);

        HttpClient client = factory.CreateClient();

        WatchStoreContext db = factory.CreateDbContext();

        Order order = Order.Create(fixture, orderOwnerId, OrderStatus.Completed);

        OrderItem orderItem = fixture.Create<OrderItem>();
        order.Items = [orderItem];

        db.Orders.Add(order);
        await db.SaveChangesAsync(CancellationToken);

        // Act
        HttpResponseMessage response = await client.GetAsync($"{OrdersEndpoint}/{order.Id}", CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /orders

    [Fact]
    public async Task GetAll_WithMultipleOrders_ReturnsPaginatedOrdersForCurrentUser()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid otherUserId = Guid.NewGuid();

        WatchStoreWebApplicationFactory factory = new(postgres, userId: userId.ToString(), scope: Scopes.ApiAccessScope);

        HttpClient client = factory.CreateClient();

        WatchStoreContext db = factory.CreateDbContext();

        // 7 ordini per l'utente test
        List<Order> userOrders = [];

        for (int i = 1; i <= 7; i++)
        {
            Order order = Order.Create(fixture, userId, OrderStatus.Completed);
            order.Items = [fixture.Create<OrderItem>()];
            userOrders.Add(order);
        }

        // 2 per un altro utente, non dovranno mischiarsi
        List<Order> otherUserOrders = [];
        for (int i = 1; i <= 2; i++)
        {
            Order order = Order.Create(fixture, userId, OrderStatus.Completed);
            order.Items = [fixture.Create<OrderItem>()];
            otherUserOrders.Add(order);
        }

        // Ordine pending per l'utente test, non dovrà essere mischiato a quelli richiesti
        Order pendingOrder = Order.Create(fixture, userId, OrderStatus.Pending);
        pendingOrder.Items = [fixture.Create<OrderItem>()];

        db.Orders.AddRange(userOrders);
        db.Orders.AddRange(otherUserOrders);
        db.Orders.Add(pendingOrder);

        await db.SaveChangesAsync(CancellationToken);

        // Act
        HttpResponseMessage response = await client.GetAsync($"{OrdersEndpoint}?pageNumber=1&pageSize=5", CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var ordersPage = await response.Content.ReadFromJsonAsync<GetOrders.OrdersPageDto>(CancellationToken);

        ordersPage.ShouldNotBeNull();
        ordersPage.TotalPages.ShouldBe(2);
        ordersPage.Data.Count().ShouldBe(5);

        // Solo i suoi
        ordersPage.Data.ShouldAllBe(o => o.CustomerId == userId);

        // Niente Pending
        ordersPage.Data.ShouldAllBe(o => o.Status != OrderStatus.Pending.ToString());
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        await postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
