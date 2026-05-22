namespace WatchStore.Api.Features.Baskets.UpsertBasket;

internal static class UpsertBasketEndpoint
{
    public const string Name = "UpsertBasket";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/{userId}", async (
            Guid userId,
            AddToBasketDto dto,
            WatchStoreContext dbContext
        ) =>
        {
            CustomerBasket? basket = await dbContext.Baskets
                                                    .Include(basket => basket.Items)
                                                    .FirstOrDefaultAsync(basket => basket.Id == userId);

            if (basket is null)
            {
                basket = new CustomerBasket
                {
                    Id = userId,
                    Items = dto.Items.Select(item => new BasketItem
                    {
                        WatchId = item.WatchId,
                        Quantity = item.Quantity
                    }).ToList()
                };

                dbContext.Baskets.Add(basket);
            }
            else
            {
                basket.Items = dto.Items.Select(item => new BasketItem
                {
                    WatchId = item.WatchId,
                    Quantity = item.Quantity
                }).ToList();
            }

            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        })
        .AddEndpointFilter<UserOrAdminFilter>()
        //doc
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .WithName(Name)
        .WithDescription("Aggiorna o crea un carrello");
    }
}
