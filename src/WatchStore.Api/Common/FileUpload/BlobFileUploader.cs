namespace WatchStore.Api.Common.Extensions;

internal sealed class BlobFileUploader(BlobServiceClient blobServiceClient, ILogger<BlobFileUploader> logger)
{
    /// <summary>
    /// Carica il file se rispetta i requisiti
    /// </summary>
    /// <returns>Risultato dell'upload <see cref="FileUploadResult"/></returns>
    public async Task<FileUploadResult> TryUploadAsync(
        IFormFile file,
        string fileName,
        string folderName,
        int maxSizeMb,
        string[] allowedExtensions)
    {
        if (file.IsEmpty())
        {
            return FileUploadResult.Fail("The file is empty");
        }

        if (file.SizeInMB > maxSizeMb)
        {
            return FileUploadResult.Fail($"The file is too big (Max is {maxSizeMb}MB)");
        }

        if (!allowedExtensions.Contains(file.Extension))
        {
            return FileUploadResult.Fail($"The file's extension ({file.Extension}) is not allowed ({string.Join(", ", allowedExtensions)})");
        }

        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(folderName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        BlobClient blobClient = containerClient.GetBlobClient(fileName);
        await blobClient.DeleteIfExistsAsync();

        using var fileStream = file.OpenReadStream();
        await blobClient.UploadAsync(
            fileStream,
            new BlobHttpHeaders { ContentType = file.ContentType }
        );

        logger.LogInformation("Image {ImageName} succesfully uploaded", fileName);

        return FileUploadResult.Success(blobClient.Uri);
    }
}
