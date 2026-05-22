namespace WatchStore.Api.Features.Watches.AddWatch;

public record NewWatchDto(
    string Name,
    decimal Price,
    DateOnly ReleaseDate,
    Guid BrandId)
{
    public IFormFile? ImageFile { get; set; }
}

public record WatchDetailsDto(
    Guid Id,
    string Name,
    decimal Price,
    DateOnly ReleaseDate,
    string ImageUri,
    string BrandName);