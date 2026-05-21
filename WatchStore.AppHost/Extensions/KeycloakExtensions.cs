namespace WatchStore.AppHost.Extensions;

internal static class KeycloakExtensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<KeycloakResource> AddKeycloak()
        {
            var keycloak = builder.AddKeycloak("keycloak", port: 8080)
                                    .WithImageTag("26.6")
                                    .WithDataVolume()
                                    .WithLifetime(ContainerLifetime.Persistent)
                                    .WithRealmImport("./watchstore-realm.json");

            return keycloak;
        }
    }
}
