namespace WatchStore.AppHost.Extensions;

internal static class ServiceBusExtension
{
    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<IResourceWithConnectionString> AddServiceBus()
        {
            var serviceBus = builder.AddAzureServiceBus(ConnectionStringsNames.ServiceBus)
                        .RunAsEmulator(emulator =>
                        {
                            emulator.WithLifetime(ContainerLifetime.Persistent);
                        });

            serviceBus.AddServiceBusQueue(QueueNames.Orders)
                        .WithProperties(queue => queue.RequiresDuplicateDetection = true);

            return serviceBus;
        }
    }
}
