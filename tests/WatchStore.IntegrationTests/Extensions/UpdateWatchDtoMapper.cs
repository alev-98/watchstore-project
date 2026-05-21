namespace WatchStore.IntegrationTests.Extensions;

public static class UpdateWatchDtoMapper
{
    extension(UpdateWatchDto dto)
    {
        /// <summary>
        /// Incapsula il dto in un contenuto Multipart pronto per essere spedito
        /// </summary>
        /// <returns>FormData appena creato</returns>
        public MultipartFormDataContent ToMultiPartFormDataContent()
        {
            MultipartFormDataContent formData = new()
            {
                { new StringContent(dto.Name), nameof(dto.Name) },
                { new StringContent(dto.BrandId.ToString()), nameof(dto.BrandId) },
                { new StringContent(dto.Price.ToString(CultureInfo.InvariantCulture)), nameof(dto.Price) },
                { new StringContent(dto.ReleaseDate.ToString("yyyy-MM-dd")), nameof(dto.ReleaseDate) }
            };

            return formData;
        }
    }
}


