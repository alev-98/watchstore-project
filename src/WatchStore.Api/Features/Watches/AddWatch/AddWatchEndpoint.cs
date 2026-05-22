namespace WatchStore.Api.Features.Watches.AddWatch;

internal static class AddWatchEndpoint
{
	public const string Name = "AddWatch";

	public static void Map(IEndpointRouteBuilder app)
	{
		app.MapPost("/", async (
			[FromForm] NewWatchDto requestDto,
			WatchStoreContext dbContext,
			BlobFileUploader fileUploader) =>
		{
			string imageUri = WatchImageConstants.ImageDefaultUri;

			if (requestDto.ImageFile is IFormFile file)
			{
				FileUploadResult result = await fileUploader
					.TryUploadImageAsync(requestDto.ImageFile);

				if (result.IsSuccessful)
				{
					imageUri = result.FileUri.ToString();
				}
				else
				{
					return Results.Problem(
						detail: result.ErrorMsg,
						statusCode: StatusCodes.Status400BadRequest);
				}
			}

			Brand? brand = await dbContext.Brands.FindAsync(requestDto.BrandId);

			Watch watch = AddWatchMapper.FromDto(requestDto, brand, imageUri);

			dbContext.Add(watch);

			await dbContext.SaveChangesAsync();

			return Results.CreatedAtRoute(
				GetWatchEndpoint.Name,
				new { id = watch.Id },
				AddWatchMapper.ToDto(watch));
		})
		.RequireAuthorization(AuthConstants.Policies.AdminAccess)
		.AddEndpointFilter<ValidationFilter<NewWatchDto>>()
		.DisableAntiforgery()
		.Accepts<NewWatchDto>(MediaTypeNames.Multipart.FormData)
		//doc
		.Produces<WatchDetailsDto>(StatusCodes.Status201Created)
		.ProducesValidationProblem()
		.ProducesProblem(StatusCodes.Status400BadRequest)
		.Produces(StatusCodes.Status401Unauthorized)
		.Produces(StatusCodes.Status403Forbidden)
		.WithName(Name)
		.WithDescription("Crea un orologio");
	}
}
