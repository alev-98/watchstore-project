using Azure.Identity;
using WatchStore.Data;
using WatchStore.Shared;
using WatchStore.Contracts.Orders;
using WatchStore.Worker.MessageHandlers;
using WatchStore.Worker.MessageHandlers.Orders;

namespace WatchStore.Worker;

public static class WorkerHostFactory
{
    public static IHost Build(
        string[]? args = null,
        string? environmentName = null,
        Action<IConfigurationBuilder>? configure = null,
        Action<IServiceCollection>? testOverrides = null)
    {
        HostApplicationBuilderSettings settings = new() { Args = args };

        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            settings.EnvironmentName = environmentName;
        }

        var builder = Host.CreateApplicationBuilder(settings);

        configure?.Invoke(builder.Configuration);

        builder.AddServiceDefaults();

        DefaultAzureCredential credential = new(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = builder.Configuration["AZURE_CLIENT_ID"]
        });

        builder.AddAzureNpgsql<WatchStoreContext>(ConnectionStringsNames.Db, credential);

        builder.AddAzureServiceBusClient(
            ConnectionStringsNames.ServiceBus,
            settings => settings.Credential = credential
        );

        builder.Services.AddScoped<IMessageHandler<OrderPaid>, OrderPaidHandler>();

        builder.Services.AddHostedService<OrdersQueueProcessor>();

        builder.Services.AddHostedService<OrdersDeadLetterQueueProcessor>();

        testOverrides?.Invoke(builder.Services);

        return builder.Build();
    }
}
