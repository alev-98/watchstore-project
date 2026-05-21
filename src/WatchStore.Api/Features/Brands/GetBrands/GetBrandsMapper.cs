namespace WatchStore.Api.Features.Brands.GetBrands;

[Mapper]
internal static partial class GetBrandsMapper
{
    public static partial BrandDto ToDto(Brand brand);
}
