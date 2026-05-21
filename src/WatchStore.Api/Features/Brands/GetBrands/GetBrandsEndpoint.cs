namespace WatchStore.Api.Features.Brands.GetBrands;

internal static class GetBrandsEndpoint
{
    public const string Name = "GetAllBrands";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (WatchStoreContext dbContext) =>
        {
            List<Brand> brands = await dbContext.Brands.ToListAsync();

            return Results.Ok(brands.Select(brand => GetBrandsMapper.ToDto(brand)));
        })
        .AllowAnonymous()
        //doc
        .Produces<List<BrandDto>>(StatusCodes.Status200OK)
        .WithName(Name)
        .WithDescription("Restituisce una lista di venditori");
    }
}
