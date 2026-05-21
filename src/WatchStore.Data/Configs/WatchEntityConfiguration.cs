namespace WatchStore.Data.Configs;

internal sealed class WatchEntityConfiguration : IEntityTypeConfiguration<Watch>
{
    public void Configure(EntityTypeBuilder<Watch> builder)
    {
        builder.Property(watch => watch.Name)
                .HasMaxLength(30);

        builder.Property(watch => watch.Price)
                .HasPrecision(5, 2);
    }
}
