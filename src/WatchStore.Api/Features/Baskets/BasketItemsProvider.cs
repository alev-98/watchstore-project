namespace WatchStore.Api.Features.Baskets;

internal class BasketItemsProvider(WatchStoreContext dbContext)
{
    public async Task<IReadOnlyList<BasketItem>> GetBasketItemsAsync(Guid userId)
    {
        CustomerBasket? basket = await dbContext.Baskets
                                                .Include(basket => basket.Items)
                                                .ThenInclude(item => item.Watch)
                                                .FirstOrDefaultAsync(basket => basket.Id == userId);

        if (basket is null)
        {
            return [];
        }

        return basket.Items.AsReadOnly();
    }
}
