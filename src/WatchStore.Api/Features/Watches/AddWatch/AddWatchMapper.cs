namespace WatchStore.Api.Features.Watches.AddWatch;

[Mapper]
internal static partial class AddWatchMapper
{
    [MapPropertyFromSource(nameof(WatchDetailsDto.BrandName), Use = nameof(MapBrandName))]
    public static partial WatchDetailsDto ToDto(Watch watch);

    private static string MapBrandName(Watch watch) => watch.Brand!.Name;

    [MapValue(nameof(Watch.Id), Use = nameof(NewGuid))]
    [MapProperty(nameof(NewWatchDto.BrandId), nameof(Watch.BrandId))]
    [MapperIgnoreSource(nameof(NewWatchDto.ImageFile))]
    public static partial Watch FromDto(NewWatchDto dto, Brand? brand, string imageUri);

    private static Guid NewGuid() => Guid.NewGuid();
}
