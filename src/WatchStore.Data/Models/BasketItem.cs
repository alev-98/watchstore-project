namespace WatchStore.Data.Models;

public class BasketItem
{
    public Guid Id { get; set; }

    public Guid WatchId { get; set; }

    public Watch? Watch { get; set; }

    public int Quantity { get; set; }

    public Guid CustomerBasketId { get; set; }
}
