namespace WatchStore.Api.Features.Watches.GetWatches;

internal static class GetWatchesEndpoint
{
    public const string Name = "GetAllWatches";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (
            [AsParameters] GetWatchesDto requestDto,
            WatchStoreContext dbContext
            ) =>
        {
            int skip = (requestDto.PageNumber - 1) * requestDto.PageSize;
            int take = requestDto.PageSize;

            IQueryable<Watch> query = dbContext.Watches.AsQueryable();

            if (!string.IsNullOrWhiteSpace(requestDto.Name))
            {
                query = query.Where(watch => EF.Functions.ILike(watch.Name, $"%{requestDto.Name}%"));
            }

            var watches = await query
                                .Include(watch => watch.Brand)
                                .OrderBy(watch => watch.Name)
                                .Skip(skip)
                                .Take(take)
                                .Select(watch => GetWatchesMapper.ToDto(watch))
                                .AsNoTracking()
                                .ToListAsync();

            int totalWatches = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalWatches / (double)requestDto.PageSize);

            return Results.Ok(new GetWatchesOutputDto(totalPages, watches));
        })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<GetWatchesDto>>()
        //doc
        .Produces<GetWatchesOutputDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .WithName(Name)
        .WithDescription("Restituisce una lista di orologi, eventualmente vuota");
    }
}
