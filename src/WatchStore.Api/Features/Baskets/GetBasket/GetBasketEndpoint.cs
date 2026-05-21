namespace WatchStore.Api.Features.Baskets.GetBasket;

internal static class GetBasketEndpoint
{
    public const string Name = "GetBasket";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/{userId}", async (
            Guid userId,
            WatchStoreContext dbContext) =>
        {
            CustomerBasket basket = await dbContext.Baskets
                                                    .Include(basket => basket.Items)
                                                    .ThenInclude(item => item.Watch)
                                                    .FirstOrDefaultAsync(basket => basket.Id == userId)
                                                        ?? new() { Id = userId };

            return Results.Ok(GetBasketMapper.ToDto(basket));
        })
        .AddEndpointFilter<UserOrAdminFilter>()
        //doc
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .WithName(Name)
        .WithDescription("Restituisce il carrello dato un id, se trovato");
    }
}
