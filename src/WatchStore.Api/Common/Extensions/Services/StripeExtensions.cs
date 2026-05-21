namespace WatchStore.Api.Common.Extensions.Services;

internal static class StripeExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddStripe(IConfiguration configuration)
        {
            IConfigurationSection stripeSection = configuration.GetSection("Stripe");

            services.AddOptions<StripeOptions>()
                    .Bind(stripeSection)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<StripeOptions>>().Value;

                return new StripeClient(options.SecretKey);
            });

            services.AddSingleton(sp =>
            {
                var client = sp.GetRequiredService<StripeClient>();

                return new SessionService(client);
            });

            services.AddSingleton(sp =>
            {
                var client = sp.GetRequiredService<StripeClient>();

                return new PaymentIntentService(client);
            });

            services.AddSingleton<IStripeEventFactory, StripeEventFactory>();

            return services;
        }
    }
}
