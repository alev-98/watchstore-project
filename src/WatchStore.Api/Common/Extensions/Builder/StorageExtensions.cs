namespace WatchStore.Api.Common.Extensions.Builder;

internal static class StorageExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddAzureStorage(
            string connectionStringName,
            TokenCredential credential
            )
        {
            builder.AddAzureBlobServiceClient(
                connectionStringName,
                settings => settings.Credential = credential
            );

            builder.Services.AddScoped<BlobFileUploader>();

            return builder;
        }
    }
}
