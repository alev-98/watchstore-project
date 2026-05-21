namespace WatchStore.Api.Features.Watches.UpdateWatch;

public record UpdateWatchDto(
    string Name,
    decimal Price,
    DateOnly ReleaseDate,
    Guid BrandId)
{
    public IFormFile? ImageFile { get; set; }
}
