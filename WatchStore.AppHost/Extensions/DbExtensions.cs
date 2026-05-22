namespace WatchStore.AppHost.Extensions;

internal static class DbExtensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<IResourceWithConnectionString> AddDb()
        {
            var db = builder.AddAzurePostgresFlexibleServer("postgres")
                            .RunAsContainer(postgres =>
                            {
                                postgres.WithHostPort(5432)
                                        .WithImageTag("18.3")
                                        .WithDataVolume()
                                        .WithLifetime(ContainerLifetime.Persistent);
                            })
                            .AddDatabase(ConnectionStringsNames.Db, "watchstore");

            return db;
        }
    }
}
