namespace WatchStore.Api.Features.Watches.GetWatches;

public record GetWatchesDto(
    int PageSize = 5,
    int PageNumber = 1,
    string? Name = null
);

public record WatchSummaryDto(
    Guid Id,
    string Name,
    decimal Price,
    string ImageUri,
    string BrandName
);

public record GetWatchesOutputDto(
    int TotalPages,
    IEnumerable<WatchSummaryDto> Data
);