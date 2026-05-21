namespace WatchStore.Api.Features.Watches.GetWatches;

public sealed class GetWatchesValidator : AbstractValidator<GetWatchesDto>
{
    public GetWatchesValidator()
    {
        RuleFor(dto => dto.PageNumber).GreaterThan(0);
        RuleFor(dto => dto.PageSize).InclusiveBetween(1, 30);
        RuleFor(dto => dto.Name).Length(0, 30);
    }
}
