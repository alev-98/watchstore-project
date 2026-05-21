namespace WatchStore.Api.Common.Extensions;

internal static class IFormFileExtensions
{
    extension(IFormFile file)
    {
        /// <summary>
        /// Estensione del file inclusa di ".", string.Empty se il file non ha estensione
        /// </summary>
        public string Extension => Path.GetExtension(file.FileName);

        /// <summary>
        /// Controlla se il file è vuoto
        /// </summary>
        public bool IsEmpty() => file.Length == 0;

        /// <summary>
        /// Dimensione del file in MB
        /// </summary>
        public decimal SizeInMB => file.Length / (1024m * 1024m);
    }
}
