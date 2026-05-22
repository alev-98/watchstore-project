namespace WatchStore.Api.Features.Baskets.UpsertBasket;

public record class AddToBasketDto(
    IEnumerable<BasketItemDto> Items);

public record BasketItemDto(
    Guid WatchId,
    int Quantity);