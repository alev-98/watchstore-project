namespace WatchStore.Api.Common.Extensions.Builder;

internal static class ServiceBusExtension
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddMessaging(
            string connectionStringName,
            TokenCredential credential
        )
        {
            builder.AddAzureServiceBusClient(
                connectionStringName,
                settings => settings.Credential = credential
            );

            builder.Services.AddSingleton<IMessagePublisher, ServiceBusMessagePublisher>();

            return builder;
        }
    }
}
