namespace WatchStore.Api.Common.Extensions.Services;

internal static class BasketExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddBasketServices()
        {
            services.AddScoped<BasketItemsProvider>()
                    .AddScoped<ClearBasketOperation>();

            return services;
        }
    }
}
