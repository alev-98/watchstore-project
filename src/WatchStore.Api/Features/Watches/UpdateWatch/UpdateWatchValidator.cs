namespace WatchStore.Api.Features.Watches.UpdateWatch;

public sealed class UpdateWatchValidator : AbstractValidator<UpdateWatchDto>
{
    public UpdateWatchValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().MinimumLength(0).MaximumLength(50);
        RuleFor(dto => dto.Price).NotEmpty().InclusiveBetween(0, decimal.MaxValue);
        RuleFor(dto => dto.ReleaseDate).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today));
    }
}
