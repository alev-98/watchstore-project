namespace WatchStore.Api.Features.Baskets.GetBasket;

[Mapper]
internal static partial class GetBasketMapper
{
    [MapProperty(nameof(CustomerBasket.Id), nameof(BasketDto.CustomerId))]
    [MapProperty(nameof(CustomerBasket.Items), nameof(BasketDto.Items))]
    public static partial BasketDto ToDto(CustomerBasket basket);

    public static BasketItemDto MapItem(BasketItem item)
    {
        return new BasketItemDto(
            item.WatchId,
            item.Watch!.Name,
            item.Watch.Price,
            item.Quantity,
            item.Watch.ImageUri
        );
    }
}
