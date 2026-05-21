namespace WatchStore.Api.Common.Extensions.Services;

internal static class OrderExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOrderServices()
        {
            services.AddScoped<OrderCreator>()
                    .AddScoped<ConfirmOrderPaymentOperation>();

            return services;
        }
    }
}
