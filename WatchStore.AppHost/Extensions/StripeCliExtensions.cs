namespace WatchStore.AppHost.Extensions;

internal static class StripeCliExtensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        public void AddStripeCliTo(
            IResourceBuilder<ProjectResource> app,
            IResourceBuilder<ParameterResource> stripeApiKey)
        {

        }
    }
}
