namespace WatchStore.Api.Features.Watches.GetWatches;

[Mapper]
internal static partial class GetWatchesMapper
{
    [MapPropertyFromSource(nameof(WatchSummaryDto.BrandName), Use = nameof(MapBrandName))]
    public static partial WatchSummaryDto ToDto(Watch watch);

    private static string MapBrandName(Watch watch) => watch.Brand!.Name;
}
