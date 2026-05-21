namespace WatchStore.IntegrationTests.Api;

public class WatchesEndpointsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:18.3").Build();

    private readonly AzuriteContainer azurite = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:3.35.0").Build();

    private readonly Fixture fixture = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string WatchesEndpoint => "/" + WatchesModule.GroupName;

    public async ValueTask InitializeAsync()
    {
        await postgres.StartAsync(CancellationToken);
        await azurite.StartAsync(CancellationToken);
        fixture.Customize<DateOnly>(o => o.FromFactory(() => DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    #region GET /watches

    [Fact]
    public async Task GetAll_WithValidRequest_ReturnsWatches()
    {
        // Arrange
        WatchStoreWebApplicationFactory factory = new(postgres, azurite);

        WatchStoreContext db = factory.CreateDbContext();

        Watch watch = Watch.Create(fixture, db);

        db.Watches.Add(watch);

        await db.SaveChangesAsync(CancellationToken);

        HttpClient client = factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(WatchesEndpoint, CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var watchesResponse = await response.Content.ReadFromJsonAsync<GetWatches.GetWatchesOutputDto>(CancellationToken);

        watchesResponse.ShouldNotBeNull();

        GetWatches.WatchSummaryDto singleWatch = watchesResponse!.Data.ShouldHaveSingleItem();

        singleWatch.ShouldNotBeNull();

        singleWatch.Id.ShouldBe(watch.Id);
        singleWatch.Name.ShouldBe(watch.Name);
        singleWatch.Price.ShouldBe(watch.Price);
        singleWatch.ImageUri.ShouldBe(watch.ImageUri);
        singleWatch.BrandName.ShouldBe(watch.Brand!.Name);
    }

    #endregion

    #region GET /watches/{id}

    [Fact]
    public async Task GetById_WithValidId_ReturnsWatch()
    {
        // Arrange
        WatchStoreWebApplicationFactory factory = new(postgres, azurite);

        WatchStoreContext db = factory.CreateDbContext();

        Watch watch = Watch.Create(fixture, db);

        db.Watches.Add(watch);

        await db.SaveChangesAsync(CancellationToken);

        HttpClient client = factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync($"{WatchesEndpoint}/{watch.Id}", CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var actual = await response.Content.ReadFromJsonAsync<GetWatch.WatchDetailsDto>(CancellationToken);

        actual.ShouldNotBeNull();

        actual.Name.ShouldBe(watch.Name);
        actual.Price.ShouldBe(watch.Price);
        actual.ImageUri.ShouldBe(watch.ImageUri);
        actual.BrandName.ShouldBe(watch.Brand!.Name);
        actual.ReleaseDate.ShouldBe(watch.ReleaseDate);
    }

    [Fact]
    public async Task GetById_WithUnexistingId_ReturnsNotFound()
    {
        // Arrange
        WatchStoreWebApplicationFactory factory = new(postgres, azurite);

        HttpClient client = factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync($"{WatchesEndpoint}/{Guid.NewGuid()}", CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /watches

    [Fact]
    public async Task Post_WithValidRequest_CreatesWatch()
    {
        // Arrange
        string userId = Guid.NewGuid().ToString();

        WatchStoreWebApplicationFactory factory = new(
                                                    postgres,
                                                    azurite,
                                                    userId: userId,
                                                    role: AuthConstants.Roles.Admin,
                                                    scope: Scopes.ApiAccessScope);

        WatchStoreContext db = factory.CreateDbContext();

        HttpClient client = factory.CreateClient();

        NewWatchDto createWatchDto = new(
                                        Name: "Test Watch",
                                        BrandId: db.Brands.First().Id,
                                        Price: 59.99m,
                                        ReleaseDate: new DateOnly(2024, 12, 25));

        MultipartFormDataContent request = createWatchDto.ToMultiPartFormDataContent();

        // Act
        HttpResponseMessage response = await client.PostAsync(WatchesEndpoint, request, CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var watchResponse = await response.Content.ReadFromJsonAsync<WatchDetailsDto>(CancellationToken);

        watchResponse.ShouldNotBeNull();

        Watch? newWatch = await db.Watches.FindAsync([watchResponse.Id], CancellationToken);

        newWatch.ShouldNotBeNull();

        newWatch.ImageUri.ShouldNotBeNullOrEmpty();
        newWatch.Name.ShouldBe(createWatchDto.Name);
        newWatch.Price.ShouldBe(createWatchDto.Price);
        newWatch.BrandId.ShouldBe(createWatchDto.BrandId);
        newWatch.ReleaseDate.ShouldBe(createWatchDto.ReleaseDate);
    }

    [Fact]
    public async Task Post_MissingRequiredInfo_ReturnsBadRequest()
    {
        // Arrange
        WatchStoreWebApplicationFactory factory = new(
                                                        postgres,
                                                        azurite,
                                                        role: AuthConstants.Roles.Admin,
                                                        scope: Scopes.ApiAccessScope);

        WatchStoreContext db = factory.CreateDbContext();

        HttpClient client = factory.CreateClient();

        NewWatchDto createWatchDto = new(
                                        Name: string.Empty,
                                        BrandId: db.Brands.First().Id,
                                        Price: 59.99m,
                                        ReleaseDate: new DateOnly(2024, 12, 25));

        MultipartFormDataContent request = createWatchDto.ToMultiPartFormDataContent();

        // Act
        HttpResponseMessage response = await client.PostAsync(WatchesEndpoint, request, CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        ProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(CancellationToken);
        object? errorsJson = problemDetails?.Extensions["errors"];
        errorsJson.ShouldNotBeNull();

        var errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(errorsJson!.ToString()!);
        errors.ShouldNotBeNull();
        errors!.ContainsKey(nameof(createWatchDto.Name)).ShouldBeTrue();
    }

    [Fact]
    public async Task Post_WithImageFile_CreatesWatchWithImageUri()
    {
        // Arrange
        WatchStoreWebApplicationFactory factory = new(
                                                    postgres,
                                                    azurite,
                                                    role: AuthConstants.Roles.Admin,
                                                    scope: Scopes.ApiAccessScope);

        WatchStoreContext db = factory.CreateDbContext();

        HttpClient client = factory.CreateClient();

        NewWatchDto createWatchDto = new(
                                        Name: "Test Watch",
                                        BrandId: db.Brands.First().Id,
                                        Price: 59.99m,
                                        ReleaseDate: new DateOnly(2024, 12, 25));

        MultipartFormDataContent request = createWatchDto.ToMultiPartFormDataContent(includeImage: true);

        // Act
        HttpResponseMessage response = await client.PostAsync(WatchesEndpoint, request, CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var watchResponse = await response.Content.ReadFromJsonAsync<WatchDetailsDto>(CancellationToken);
        watchResponse.ShouldNotBeNull();
        watchResponse!.Id.ShouldNotBe(Guid.Empty);

        Watch? createdWatch = await db.Watches.FindAsync([watchResponse?.Id], CancellationToken);

        createdWatch.ShouldNotBeNull();

        createdWatch!.ImageUri.ShouldNotBeNullOrWhiteSpace();

        HttpClient imgClient = new();

        HttpResponseMessage imgResponse = await imgClient.GetAsync(createdWatch.ImageUri, CancellationToken);

        imgResponse.EnsureSuccessStatusCode();
        imgResponse.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
    }

    #endregion

    #region PUT watches/{id}

    [Fact]
    public async Task Put_WithValidRequest_UpdatesWatch()
    {
        // Arrange
        string userId = Guid.NewGuid().ToString();

        WatchStoreWebApplicationFactory factory = new(
                                                    postgres,
                                                    azurite,
                                                    userId: userId,
                                                    role: AuthConstants.Roles.Admin,
                                                    scope: Scopes.ApiAccessScope);

        WatchStoreContext db = factory.CreateDbContext();

        Watch watch = Watch.Create(fixture, db);

        db.Watches.Add(watch);

        await db.SaveChangesAsync(CancellationToken);

        HttpClient client = factory.CreateClient();

        UpdateWatchDto updateWatchDto = new(
                                            Name: "Updated Watch",
                                            BrandId: db.Brands.First().Id,
                                            Price: 39.99m,
                                            ReleaseDate: new DateOnly(2024, 12, 25));

        MultipartFormDataContent request = updateWatchDto.ToMultiPartFormDataContent();

        // Act
        HttpResponseMessage response = await client.PutAsync($"{WatchesEndpoint}/{watch.Id}", request, CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        db = factory.CreateDbContext();

        Watch? updatedWatch = await db.Watches.FindAsync([watch.Id], CancellationToken);

        updatedWatch.ShouldNotBeNull();

        updatedWatch!.Name.ShouldBe(updateWatchDto.Name);
        updatedWatch.Price.ShouldBe(updateWatchDto.Price);
        updatedWatch.BrandId.ShouldBe(updateWatchDto.BrandId);
        updatedWatch.ReleaseDate.ShouldBe(updateWatchDto.ReleaseDate);
    }

    [Fact]
    public async Task Put_NoAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        WatchStoreWebApplicationFactory application = new(postgres, authenticationSucceeds: false);

        WatchStoreContext db = application.CreateDbContext();

        HttpClient client = application.CreateClient();

        UpdateWatchDto updateWatchDto = new(
                                            Name: "Updated Watch",
                                            BrandId: db.Brands.First().Id,
                                            Price: 39.99m,
                                            ReleaseDate: DateOnly.FromDateTime(DateTime.UtcNow)
                                        );

        MultipartFormDataContent request = updateWatchDto.ToMultiPartFormDataContent();

        // Act
        HttpResponseMessage response = await client.PutAsync($"{WatchesEndpoint}/{Guid.NewGuid()}", request, CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region DELETE /watch

    [Fact]
    public async Task Delete_WithValidId_DeletesWatch()
    {
        // Arrange
        WatchStoreWebApplicationFactory factory = new(postgres, role: AuthConstants.Roles.Admin, scope: Scopes.ApiAccessScope);

        WatchStoreContext db = factory.CreateDbContext();

        Watch watch = Watch.Create(fixture, db);

        db.Watches.Add(watch);

        await db.SaveChangesAsync(CancellationToken);

        HttpClient client = factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"{WatchesEndpoint}/{watch.Id}", CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        db = factory.CreateDbContext();

        Watch? watchInDb = await db.Watches.FindAsync([watch.Id], CancellationToken);

        watchInDb.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_UnauthorizedRole_ReturnsForbidden()
    {
        // Arrange
        WatchStoreWebApplicationFactory factory = new(postgres, role: "TestRole", scope: Scopes.ApiAccessScope);

        HttpClient client = factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"{WatchesEndpoint}/{Guid.NewGuid()}", CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        await postgres.DisposeAsync();
        await azurite.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
