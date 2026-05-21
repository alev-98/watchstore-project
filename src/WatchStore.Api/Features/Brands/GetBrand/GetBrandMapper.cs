namespace WatchStore.Api.Features.Brands.GetBrand;

[Mapper]
internal static partial class GetBrandMapper
{
    public static partial BrandDto ToDto(Brand brand);
}
