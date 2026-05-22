namespace WatchStore.Api.Features.Orders.GetOrders;

internal static class GetOrdersEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (
            [AsParameters] GetOrdersDto request,
            WatchStoreContext dbContext,
            ClaimsPrincipal user
        ) =>
        {
            string? userIdClaimValue = user.FindFirstValue(AuthConstants.Claims.UserId);

            if (!Guid.TryParse(userIdClaimValue, out Guid userId))
            {
                return Results.Forbid();
            }

            var filteredOrders = dbContext.Orders
                                            .Where(order => order.CustomerId == userId
                                                    && order.Status != OrderStatus.Pending);

            int skip = (request.PageNumber - 1) * request.PageSize;
            int take = request.PageSize;

            List<OrderDto> ordersOnPage = await filteredOrders
                                                .OrderByDescending(order => order.Created)
                                                .Include(order => order.Items)
                                                .Skip(skip)
                                                .Take(take)
                                                .Select(order => new OrderDto(
                                                    order.Id,
                                                    order.OrderNumber,
                                                    order.CustomerId,
                                                    order.Created,
                                                    order.Status.ToString(),
                                                    order.TotalAmount,
                                                    order.Items.Select(item => new OrderItemDto(
                                                        item.ProductId,
                                                        item.ProductName,
                                                        item.Price,
                                                        item.Quantity,
                                                        item.ImageUri
                                                    ))
                                                ))
                                                .AsNoTracking()
                                                .ToListAsync();

            int totalOrders = await filteredOrders.CountAsync();
            int totalPages = (int)Math.Ceiling(totalOrders / (double)request.PageSize);

            OrdersPageDto dto = new(totalPages, ordersOnPage);

            return Results.Ok(dto);
        });
    }
}
