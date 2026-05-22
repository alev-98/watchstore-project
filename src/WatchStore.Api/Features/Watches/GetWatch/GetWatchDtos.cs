namespace WatchStore.Api.Features.Watches.GetWatch;

public record WatchDetailsDto(
    string Name,
    decimal Price,
    DateOnly ReleaseDate,
    string ImageUri,
    string BrandName);