namespace WatchStore.IntegrationTests.Api;

public class BasketEndpointsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:18.3").Build();

    private readonly Fixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string BasketsEndpoint => "/" + BasketsModule.GroupName;

    public async ValueTask InitializeAsync()
    {
        await postgres.StartAsync();
        fixture.Customize<DateOnly>(o => o.FromFactory(() => DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    #region GET /baskets/{id}

    [Fact]
    public async Task GetById_WithExistingBasket_ReturnsBasket()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        WatchStoreWebApplicationFactory factory = new(postgres, userId: userId.ToString(), scope: Scopes.ApiAccessScope);

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

        HttpClient client = factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync($"{BasketsEndpoint}/{userId}", CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var basketResponse = await response.Content.ReadFromJsonAsync<GetBasket.BasketDto>(CancellationToken);

        basketResponse?.CustomerId.ShouldBe(basket.Id);
        basketResponse?.Items.Count().ShouldBe(1);

        GetBasket.BasketItemDto? item = basketResponse?.Items.First();

        item.ShouldNotBeNull();
        item.Id.ShouldBe(watch.Id);
        item.Quantity.ShouldBe(basket.Items.First().Quantity);
        item.Name.ShouldBe(watch.Name);
        item.Price.ShouldBe(watch.Price);
    }

    [Fact]
    public async Task GetById_WithDifferentUserId_ReturnsForbidden()
    {
        // Arrange
        Guid basketOwnerId = Guid.NewGuid();
        Guid differentUserId = Guid.NewGuid();

        WatchStoreWebApplicationFactory factory = new(postgres, userId: differentUserId.ToString(), scope: Scopes.ApiAccessScope);

        WatchStoreContext db = factory.CreateDbContext();

        Watch watch = Watch.Create(fixture, db);

        CustomerBasket basket = new()
        {
            Id = basketOwnerId,
            Items =
            [
                new() { WatchId = watch.Id, Quantity = 1 }
            ]
        };

        db.Watches.Add(watch);
        db.Baskets.Add(basket);
        await db.SaveChangesAsync(CancellationToken);

        HttpClient client = factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync($"{BasketsEndpoint}/{basketOwnerId}", CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    #region PUT /baskets/{id}

    [Fact]
    public async Task UpsertBasket_CreatesBasket()
    {
        // Arrange
        Guid userId = Guid.NewGuid();

        WatchStoreWebApplicationFactory factory = new(postgres, userId: userId.ToString(), scope: Scopes.ApiAccessScope);

        WatchStoreContext db = factory.CreateDbContext();

        Watch watch1 = Watch.Create(fixture, db);

        Watch watch2 = Watch.Create(fixture, db);

        db.Watches.AddRange(watch1, watch2);

        await db.SaveChangesAsync(CancellationToken);

        HttpClient client = factory.CreateClient();

        // Act
        UpsertBasket.AddToBasketDto upsertDto = new(
        [
            new UpsertBasket.BasketItemDto(watch1.Id, 2),
            new UpsertBasket.BasketItemDto(watch2.Id, 1)
        ]);

        HttpResponseMessage putResponse = await client.PutAsJsonAsync($"{BasketsEndpoint}/{userId}", upsertDto, CancellationToken);

        // Assert
        putResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        CustomerBasket? basketInDb = await db.Baskets
                                                .Include(b => b.Items)
                                                .FirstOrDefaultAsync(b => b.Id == userId, CancellationToken);

        basketInDb.ShouldNotBeNull();
        basketInDb!.Items.Count.ShouldBe(2);

        basketInDb.Items.Any(i => i.WatchId == watch1.Id && i.Quantity == 2).ShouldBeTrue();
        basketInDb.Items.Any(i => i.WatchId == watch2.Id && i.Quantity == 1).ShouldBeTrue();
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        await postgres.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
