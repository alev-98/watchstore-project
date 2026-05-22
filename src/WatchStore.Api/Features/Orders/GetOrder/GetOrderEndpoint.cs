namespace WatchStore.Api.Features.Orders.GetOrder;

internal static class GetOrderEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/{id}", async (
            Guid id,
            WatchStoreContext dbContext,
            ClaimsPrincipal user
        ) =>
        {
            string? userIdClaimValue = user.FindFirstValue(AuthConstants.Claims.UserId);

            if (!Guid.TryParse(userIdClaimValue, out Guid userId))
            {
                return Results.Forbid();
            }

            Order? order = await dbContext.Orders
                                            .Include(order => order.Items)
                                            .FirstOrDefaultAsync(order => order.Id == id);

            if (order is null)
            {
                return Results.NotFound();
            }

            if (userId != order.CustomerId)
            {
                return Results.Forbid();
            }

            OrderDto dto = new(
                order.Id,
                order.OrderNumber,
                order.CustomerId,
                order.Created,
                order.Status.ToString(),
                order.TotalAmount,
                order.PaymentCardBrand,
                order.PaymentCardLast4,
                order.Items.Select(item => new OrderItemDto(
                    item.ProductId,
                    item.ProductName,
                    item.Price,
                    item.Quantity,
                    item.ImageUri,
                    item.WatchCodes
                ))
            );

            return Results.Ok(dto);
        });
    }
}
