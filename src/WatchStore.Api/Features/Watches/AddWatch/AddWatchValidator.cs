namespace WatchStore.Api.Features.Watches.AddWatch;

public sealed class AddWatchValidator : AbstractValidator<NewWatchDto>
{
    public AddWatchValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().MinimumLength(0).MaximumLength(30);
        RuleFor(dto => dto.Price).NotEmpty().InclusiveBetween(0, decimal.MaxValue);
        RuleFor(dto => dto.ReleaseDate).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today));
    }
}
