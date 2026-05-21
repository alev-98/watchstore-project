namespace WatchStore.Api.Features.Watches.GetWatch;

[Mapper]
internal static partial class GetWatchMapper
{
    [MapperIgnoreSource(nameof(Watch.Id))]
    [MapPropertyFromSource(nameof(WatchDetailsDto.BrandName), Use = nameof(MapBrandName))]
    public static partial WatchDetailsDto ToDto(Watch watch);

    private static string MapBrandName(Watch watch) => watch.Brand!.Name;
}
