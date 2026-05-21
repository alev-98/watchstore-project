namespace WatchStore.Api.Features.Brands.GetBrand;

internal static class GetBrandEndpoint
{
    public const string Name = "GetBrand";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/{id}", async (Guid id, WatchStoreContext dbContext) =>
        {
            Brand? brand = await dbContext.Brands.FindAsync(id);

            return brand is null ? Results.NotFound() : Results.Ok(GetBrandMapper.ToDto(brand));
        })
        .AllowAnonymous()
        //doc
        .Produces<BrandDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName(Name)
        .WithDescription("Restituisce una lista di venditori");
    }
}
