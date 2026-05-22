namespace WatchStore.AppHost.Extensions;

internal static class StorageExstensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        public IResourceBuilder<IResourceWithConnectionString> AddStorage()
        {
            var blobs = builder.AddAzureStorage("storage")
                                .ConfigureInfrastructure(infra =>
                                {
                                    var storageAccount = infra.GetProvisionableResources()
                                                                .OfType<StorageAccount>()
                                                                .Single();

                                    storageAccount.AllowBlobPublicAccess = true;
                                    storageAccount.Sku = new StorageSku { Name = StorageSkuName.StandardLrs };
                                })
                                .RunAsEmulator(storage =>
                                {
                                    storage.WithBlobPort(10000)
                                        .WithImageTag("3.35.0")
                                        .WithDataVolume()
                                        .WithLifetime(ContainerLifetime.Persistent);
                                })
                                .AddBlobs(ConnectionStringsNames.StorageBlobs);

            return blobs;
        }
    }
}
