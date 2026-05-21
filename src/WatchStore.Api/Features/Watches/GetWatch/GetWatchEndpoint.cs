namespace WatchStore.Api.Features.Watches.GetWatch;

internal static class GetWatchEndpoint
{
    public const string Name = "GetWatch";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/{id}", async (Guid id, WatchStoreContext dbContext) =>
        {
            Watch? watch = await dbContext.Watches
                                            .Where(watch => watch.Id == id)
                                            .Include(watch => watch.Brand)
                                            .FirstOrDefaultAsync();

            return watch is null
                ? Results.NotFound()
                : Results.Ok(GetWatchMapper.ToDto(watch));
        })
        .AllowAnonymous()
        //doc
        .Produces<WatchDetailsDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName(Name)
        .WithDescription("Restituisce un orologio dato un id, se trovato");
    }
}
