namespace WatchStore.Api.Features.Watches.UpdateWatch;

internal static class UpdateWatchEndpoint
{
    public const string Name = "UpdateWatch";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/{id}", async (
            [FromForm] UpdateWatchDto requestDto,
            Guid id,
            WatchStoreContext dbContext,
            BlobFileUploader fileUploader) =>
        {
            Watch? watchToUpdate = await dbContext.Watches
                                                    .Include(watch => watch.Brand)
                                                    .FirstOrDefaultAsync(watch => watch.Id == id);

            if (watchToUpdate is null)
            {
                return Results.NotFound();
            }

            if (requestDto.ImageFile is IFormFile file)
            {
                FileUploadResult result = await fileUploader
                    .TryUploadImageAsync(requestDto.ImageFile);

                if (result.IsSuccessful)
                {
                    watchToUpdate.ImageUri = result.FileUri.ToString();
                }
                else
                {
                    return Results.Problem(
                        detail: result.ErrorMsg,
                        statusCode: StatusCodes.Status400BadRequest);
                }
            }

            watchToUpdate.Name = requestDto.Name;
            watchToUpdate.Price = requestDto.Price;
            watchToUpdate.ReleaseDate = requestDto.ReleaseDate;
            watchToUpdate.BrandId = requestDto.BrandId;

            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization(AuthConstants.Policies.AdminAccess)
        .AddEndpointFilter<ValidationFilter<UpdateWatchDto>>()
        .DisableAntiforgery()
        .Accepts<UpdateWatchDto>(MediaTypeNames.Multipart.FormData)
        //doc
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .WithName(Name)
        .WithDescription("Modifica un orologio dato un id");
    }
}
