namespace WatchStore.Data;

public sealed class WatchStoreContext(
    DbContextOptions<WatchStoreContext> options
    ) : DbContext(options)
{
    public DbSet<Watch> Watches { get; set; }

    public DbSet<Brand> Brands { get; set; }

    public DbSet<BasketItem> BasketItems { get; set; }

    public DbSet<CustomerBasket> Baskets { get; set; }

    public DbSet<Order> Orders { get; set; }

    public DbSet<OrderItem> OrderItems { get; set; }

    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasSequence<long>(OrderEntityConfiguration.OrderNumbersSequence)
                    .StartsAt(1)
                    .IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WatchEntityConfiguration).Assembly);
    }
}
