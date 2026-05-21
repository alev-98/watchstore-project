namespace WatchStore.Api.Common.Extensions.App;

internal static class MigrationExtensions
{
    extension(WebApplication app)
    {
        /// <summary>
        /// Effettua migrations db
        /// </summary>
        /// <typeparam name="TContext">Context del Db su cui effettuare migs</typeparam>
        public async Task MigrateDbAsync<TContext>() where TContext : DbContext
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

            await dbContext.Database.MigrateAsync();
        }
    }
}
