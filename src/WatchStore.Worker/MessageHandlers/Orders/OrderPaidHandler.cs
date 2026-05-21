using WatchStore.Data;
using WatchStore.Data.Models;
using WatchStore.Contracts.Orders;
using Microsoft.EntityFrameworkCore;

namespace WatchStore.Worker.MessageHandlers.Orders;

public class OrderPaidHandler(
    WatchStoreContext context,
    TimeProvider timeProvider,
    ILogger<OrderPaidHandler> logger
) : IMessageHandler<OrderPaid>
{
    public async Task HandleAsync(OrderPaid orderPaid, CancellationToken ct = default)
    {
        if (orderPaid is null || orderPaid.OrderId == Guid.Empty)
        {
            logger.LogWarning("Ignoring invalid OrderPaid message.");

            return;
        }

        var orderId = orderPaid.OrderId;

        logger.LogInformation("Processing OrderPaid for Order {OrderId}", orderId);

        var order = await context.Orders
                                    .Include(order => order.Items)
                                    .FirstOrDefaultAsync(order => order.Id == orderId, ct);

        if (order is null)
        {
            logger.LogError("Order not found.");

            return;
        }

        if (order.Status != OrderStatus.Processing)
        {
            logger.LogWarning("Order {OrderId} is not in Processing status. Status: {Status}",
                                order.Id,
                                order.Status);

            return;
        }

        foreach (var item in order.Items)
        {
            item.WatchCodes = WatchCodeGenerator.GenerateCodes(item.Quantity);
        }

        order.Status = OrderStatus.Completed;
        order.LastUpdated = timeProvider.GetUtcNow();

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Successfully assigned watch codes for order {OrderId}",
                                orderId);
    }
}
