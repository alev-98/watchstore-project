using Npgsql;

namespace WatchStore.Api.Features.Orders.CreateOrder;

internal class OrderCreator(
    WatchStoreContext dbContext,
    BasketItemsProvider basketItemsProvider,
    TimeProvider timeProvider,
    ILogger<OrderCreator> logger
)
{
    public async Task<CreateOrderResult> GetOrCreateOrderAsync(
        Guid userId,
        Guid operationId)
    {
        Order? existingOrder = await dbContext.Orders
                                                .Include(order => order.Items)
                                                .FirstOrDefaultAsync(order => order.OperationId == operationId
                                                                        && order.CustomerId == userId);

        if (existingOrder is not null)
        {
            return new CreateOrderResult(existingOrder);
        }

        IReadOnlyList<BasketItem> basketItems = await basketItemsProvider.GetBasketItemsAsync(userId);

        if (!basketItems.Any())
        {
            return new CreateOrderResult(EmptyBasket: true);
        }

        DateTimeOffset now = timeProvider.GetUtcNow();

        Order order = new()
        {
            CustomerId = userId,
            Created = now,
            Status = OrderStatus.Pending,
            LastUpdated = now,
            Items = [.. basketItems.Select(basketItem => new OrderItem{
                ProductId = basketItem.WatchId,
                ProductName = basketItem.Watch!.Name,
                Quantity = basketItem.Quantity,
                Price = basketItem.Watch.Price,
                ImageUri = basketItem.Watch.ImageUri
            })],
            OperationId = operationId
        };

        dbContext.Orders.Add(order);

        try
        {
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Created new order {OrderId} for user {UserId} and OperationId {OprationId}",
                                    order.Id,
                                    userId,
                                    operationId);

            return new CreateOrderResult(order);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException pgEx
            && pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            dbContext.Entry(order).State = EntityState.Detached;

            logger.LogWarning(ex, "Database update failed for OperationId {OperationId} and UserId {UserId}. Checking for exising order.",
                                operationId,
                                userId);

            Order? raceConditionOrder = await dbContext.Orders
                                                        .Include(order => order.Items)
                                                        .FirstOrDefaultAsync(order => order.OperationId == operationId
                                                                        && order.CustomerId == userId);

            if (raceConditionOrder is not null)
            {
                logger.LogInformation("Found existing order {OrderId} for OperationId {OperationId}",
                                        raceConditionOrder.Id,
                                        operationId);

                return new CreateOrderResult(raceConditionOrder);
            }

            logger.LogError(ex, "No existing order found after DbUpdateException for OperationId {OperationId}",
                                operationId);

            throw;
        }
    }
}
