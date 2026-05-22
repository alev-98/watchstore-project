#region Builder

using WatchStore.Api.Common.Outbox;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

DefaultAzureCredential credential = new(new DefaultAzureCredentialOptions
{
    ManagedIdentityClientId = builder.Configuration["AZURE_CLIENT_ID"]
});

builder.AddAzureNpgsql<WatchStoreContext>(ConnectionStringsNames.Db, credential);

builder.AddAzureStorage(ConnectionStringsNames.StorageBlobs, credential);

builder.AddMessaging(ConnectionStringsNames.ServiceBus, credential);

builder.Services.AddHostedService<OutboxProcessor>();

builder.Services.AddAuthorizationPolicies();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddKeycloakAuthentication();
}

if (builder.Environment.IsProduction())
{
    builder.Configuration.AddAzureKeyVaultSecrets(
        "keyvault",
        settings => settings.Credential = credential);

    builder.Services.AddEntraAuthentication();
}

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.RequestMethod |
                            HttpLoggingFields.RequestPath |
                            HttpLoggingFields.ResponseStatusCode |
                            HttpLoggingFields.Duration;

    options.CombineLogs = true;
});

builder.Services.AddOpenApi();
builder.Services.AddCarter();

builder.Services.AddStripe(builder.Configuration);

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddOrderServices();
builder.Services.AddBasketServices();

#endregion

#region App

WebApplication app = builder.Build();

app.UseStatusCodePages();
app.UseAuthorization();
app.UseWhen(
    context => context.Request.Path.ToString() is not "/health" and not "/alive",
    appBuilder => appBuilder.UseHttpLogging()
);
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi()
        .AllowAnonymous();
    app.MapScalarApiReference()
        .AllowAnonymous();
}

app.MapCarter();
app.MapDefaultEndpoints();

await app.MigrateDbAsync<WatchStoreContext>();

app.Run();

#endregion
