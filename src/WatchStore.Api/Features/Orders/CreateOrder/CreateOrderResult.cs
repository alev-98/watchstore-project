namespace WatchStore.Api.Features.Orders.CreateOrder;

public record CreateOrderResult(
    Order? Order = null,
    [property: MemberNotNullWhen(returnValue: false, member: nameof(Order))]
    bool EmptyBasket = false
);
