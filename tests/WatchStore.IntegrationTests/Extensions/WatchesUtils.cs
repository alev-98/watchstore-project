namespace WatchStore.IntegrationTests.Extensions;

/// <summary>
/// Metodi utility per <see cref="Watch"/>
/// </summary>
internal static class WatchesUtils
{
    extension(Watch watch)
    {
        /// <summary>
        /// Crea un Watch con valori casuali ma validi
        /// </summary>
        /// <param name="fixture">Creazione oggetto</param>
        /// <param name="dbContext">Recuperare un Brand</param>
        /// <returns>Watch appena creato</returns>
        public static Watch Create(Fixture fixture, WatchStoreContext dbContext)
        {
            return fixture.Build<Watch>()
                                    .With(w => w.Brand, dbContext.Brands.First())
                                    .With(w => w.Name, fixture.Create<string>()[..30])
                                    .Create();
        }
    }
}
