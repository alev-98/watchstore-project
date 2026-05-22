namespace WatchStore.Api.Common.Extensions;

internal static class BlobFileUploaderImageExtension
{
    extension(BlobFileUploader uploader)
    {
        /// <summary>
        /// Prova a caricare un file come immagine, assegnando un nome sicuro e
        /// utilizzando le costanti in <see cref="WatchImageConstants"/>
        /// </summary>
        /// <returns>Risultato dell'upload <see cref="FileUploadResult"/></returns>
        public async Task<FileUploadResult> TryUploadImageAsync(IFormFile file)
        {
            return await uploader.TryUploadAsync(
                file: file,
                fileName: $"{Guid.NewGuid()}{file.Extension}",
                folderName: WatchImageConstants.ImagesFolderName,
                maxSizeMb: WatchImageConstants.ImageMaxSizeMb,
                allowedExtensions: WatchImageConstants.ImageAllowedExtensions);
        }
    }
}
