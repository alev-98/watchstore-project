namespace WatchStore.Data.Configs;

internal sealed class BrandEntityConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.Property(brand => brand.Name)
                .HasMaxLength(30);
    }
}
