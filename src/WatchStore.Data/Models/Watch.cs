namespace WatchStore.Data.Models;

public class Watch
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public decimal Price { get; set; }

    public DateOnly ReleaseDate { get; set; }

    public required string ImageUri { get; set; }

    public Brand? Brand { get; set; }

    public Guid BrandId { get; set; }
}
