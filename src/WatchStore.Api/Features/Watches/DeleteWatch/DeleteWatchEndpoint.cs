namespace WatchStore.Api.Features.Watches.DeleteWatch;

internal static class DeleteWatchEndpoint
{
    public const string Name = "DeleteWatch";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/{id}", async (Guid id, WatchStoreContext dbContext) =>
        {
            await dbContext.Watches.Where(b => b.Id == id)
                                    .ExecuteDeleteAsync();

            return Results.NoContent();
        })
        .RequireAuthorization(AuthConstants.Policies.AdminAccess)
        //doc
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .WithName(Name)
        .WithDescription("Elimina un orologio dato un id");
    }
}
