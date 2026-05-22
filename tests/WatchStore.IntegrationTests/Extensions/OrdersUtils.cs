namespace WatchStore.IntegrationTests.Extensions;

/// <summary>
/// Metodi utility per <see cref="Order"/>
/// </summary>
internal static class OrdersUtils
{
    extension(Order order)
    {
        /// <summary>
        /// Crea un order con valori casuali ma validi
        /// </summary>
        /// <param name="fixture">Creatore ordine</param>
        /// <param name="userId">UserId associato all'ordine</param>
        /// <param name="status">Stato dell'ordine</param>
        /// <returns>Order appena creato</returns>
        public static Order Create(Fixture fixture, Guid userId, OrderStatus status)
        {
            return fixture.Build<Order>()
                            .With(o => o.CustomerId, userId)
                            .With(o => o.Status, status)
                            .Without(o => o.Items)
                            .Create();
        }
    }
}
