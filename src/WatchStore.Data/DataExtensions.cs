namespace WatchStore.Data;

public static class DbExtensions
{
    extension(IHostApplicationBuilder builder)
    {
        public void AddAzureNpgsql<TContext>(
            string connectionStringName,
            TokenCredential credential
            ) where TContext : DbContext
        {
            if (builder.Environment.IsProduction())
            {
                builder.AddAzureNpgsqlDbContext<TContext>(
                    connectionStringName,
                    settings => settings.Credential = credential,
                    configureDbContextOptions: options => UseSeeding(options));
            }
            else
            {
                builder.AddNpgsqlDbContext<TContext>(
                    connectionStringName,
                    configureDbContextOptions: options => UseSeeding(options));
            }
        }
    }

    private static DbContextOptionsBuilder UseSeeding(DbContextOptionsBuilder options)
    {
        return options.UseSeeding((context, _) =>
        {
            if (!context.Set<Brand>().Any())
            {
                SeedBrands(context);
                context.SaveChanges();
            }
        })
        .UseAsyncSeeding(async (context, _, cancellationToken) =>
        {
            if (!context.Set<Brand>().Any())
            {
                SeedBrands(context);
                await context.SaveChangesAsync(cancellationToken);
            }
        });
    }

    private static void SeedBrands(DbContext context)
    {
        context.Set<Brand>().AddRange(
            new() { Name = "Casio" },
            new() { Name = "Citizen" },
            new() { Name = "Seiko" },
            new() { Name = "Maserati" },
            new() { Name = "Sector" }
        );
    }
}
