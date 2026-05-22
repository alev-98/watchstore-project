namespace WatchStore.IntegrationTests.Extensions;

public static class CreateWatchDtoMapper
{
    extension(NewWatchDto dto)
    {
        /// <summary>
        /// Incapsula il dto in un contenuto Multipart pronto per essere spedito
        /// </summary>
        /// <param name="includeImage">Se includere o meno immagine</param>
        /// <returns>FormData appena creato</returns>
        public MultipartFormDataContent ToMultiPartFormDataContent(bool includeImage = false)
        {
            MultipartFormDataContent formData = new()
            {
                { new StringContent(dto.Name), nameof(dto.Name) },
                { new StringContent(dto.BrandId.ToString()), nameof(dto.BrandId) },
                { new StringContent(dto.Price.ToString(CultureInfo.InvariantCulture)), nameof(dto.Price) },
                { new StringContent(dto.ReleaseDate.ToString("yyyy-MM-dd")), nameof(dto.ReleaseDate) },
            };

            if (includeImage)
            {
                ByteArrayContent imageFileContent = new([1, 2, 3]);

                imageFileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");

                formData.Add(imageFileContent, "ImageFile", $"{Guid.NewGuid()}.png");
            }

            return formData;
        }
    }
}


