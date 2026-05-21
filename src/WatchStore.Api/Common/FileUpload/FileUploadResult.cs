namespace WatchStore.Api.Common.Extensions;

/// <summary>
/// Rappresenta il risultato dell'Upload di un file
/// </summary>
internal record struct FileUploadResult(
    [property: MemberNotNullWhen(true, "FileUri")]
    [property: MemberNotNullWhen(false, "ErrorMsg")]
    bool IsSuccessful,
    Uri? FileUri,
    string? ErrorMsg
)
{
    /// <summary>
    /// Crea un FileUploadResult che indica successo
    /// </summary>
    /// <param name="fileUri">Uri del file</param>
    /// <returns>L'oggetto appena creato</returns>
    public static FileUploadResult Success(Uri fileUri) => new(true, fileUri, null);

    /// <summary>
    /// Crea un FileUploadResult che indica fallimento
    /// </summary>
    /// <param name="errorMsg">Errore riscontrato</param>
    /// <returns>L'oggetto appena creato</returns>
    public static FileUploadResult Fail(string errorMsg) => new(false, null, errorMsg);
}
